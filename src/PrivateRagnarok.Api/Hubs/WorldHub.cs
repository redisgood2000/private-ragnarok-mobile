using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrivateRagnarok.Api.Data;
using PrivateRagnarok.Api.Models;

namespace PrivateRagnarok.Api.Hubs;

/// <summary>
/// Single-process authoritative movement foundation. Clients submit direction intent,
/// while the server owns position, character progression, and persistence.
/// </summary>
public sealed class WorldHub(GameDbContext db, ILogger<WorldHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, PlayerSession> Online = new();
    private const float WalkSpeed = 4.5f;
    private const float WorldHalfExtent = 28f;
    private const double MaxSimulationStepSeconds = 0.25;
    private const double MinimumInputIntervalSeconds = 0.05;

    public override async Task OnConnectedAsync()
    {
        var playerIdText = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = Context.User?.Identity?.Name;
        if (!Guid.TryParse(playerIdText, out var playerId) || string.IsNullOrWhiteSpace(username))
        {
            Context.Abort();
            return;
        }

        // Idempotent bootstrap supports existing accounts without resetting saved characters.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "PlayerCharacters"
                ("PlayerId", "CharacterName", "JobClass", "Level", "BaseExperience",
                 "X", "Y", "Z", "Yaw", "UpdatedAt")
            VALUES ({playerId}, {username}, 'Novice', 1, 0, 0, 0, -7, 0, {DateTimeOffset.UtcNow})
            ON CONFLICT ("PlayerId") DO NOTHING
            """);

        var character = await db.Characters.AsNoTracking()
            .SingleAsync(x => x.PlayerId == playerId);
        var session = new PlayerSession(Context.ConnectionId, playerIdText!, character.CharacterName,
            character.Level, character.JobClass, character.X, character.Y, character.Z, character.Yaw);
        Online.AddOrUpdate(playerIdText!, session, (_, _) => session);

        await Clients.Caller.SendAsync("PlayerSpawned", session.Snapshot());
        await Clients.Caller.SendAsync("OnlinePlayers",
            Online.Values.Where(x => x.PlayerId != playerIdText).Select(x => x.Snapshot()).ToArray());
        await Clients.Others.SendAsync("PlayerJoined", session.Snapshot());
        await base.OnConnectedAsync();
    }

    /// <summary>Submit normalized movement intent. X/Z are direction inputs, never world coordinates.</summary>
    public async Task MoveInput(MoveInputRequest request)
    {
        if (request is null || !float.IsFinite(request.X) || !float.IsFinite(request.Z))
            throw new HubException("Invalid movement input.");

        var playerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(playerId) ||
            !Online.TryGetValue(playerId, out var session) ||
            session.ConnectionId != Context.ConnectionId)
            throw new HubException("Player session not found.");

        PlayerSnapshot? snapshot = null;
        lock (session.Sync)
        {
            if (request.Sequence <= session.LastSequence) return;
            session.LastSequence = request.Sequence;

            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(session.LastInputTimestamp, now).TotalSeconds;
            if (elapsed < MinimumInputIntervalSeconds) return;
            var dt = Math.Clamp(elapsed, 0d, MaxSimulationStepSeconds);
            session.LastInputTimestamp = now;

            var inputX = Math.Clamp(request.X, -1f, 1f);
            var inputZ = Math.Clamp(request.Z, -1f, 1f);
            var length = MathF.Sqrt(inputX * inputX + inputZ * inputZ);
            if (length > 1f)
            {
                inputX /= length;
                inputZ /= length;
            }

            if (length > 0.001f)
            {
                session.X = Math.Clamp(session.X + inputX * WalkSpeed * (float)dt,
                    -WorldHalfExtent, WorldHalfExtent);
                session.Z = Math.Clamp(session.Z + inputZ * WalkSpeed * (float)dt,
                    -WorldHalfExtent, WorldHalfExtent);
                session.Yaw = MathF.Atan2(inputX, inputZ) * RadiansToDegrees;
                snapshot = session.Snapshot();
            }
        }

        if (snapshot is not null)
            await Clients.All.SendAsync("PlayerMoved", snapshot);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(playerId) &&
            Online.TryGetValue(playerId, out var session) &&
            session.ConnectionId == Context.ConnectionId &&
            ((ICollection<KeyValuePair<string, PlayerSession>>)Online).Remove(
                new KeyValuePair<string, PlayerSession>(playerId, session)))
        {
            try
            {
                var character = await db.Characters.SingleOrDefaultAsync(x => x.PlayerId.ToString() == playerId);
                if (character is not null)
                {
                    lock (session.Sync)
                    {
                        character.X = session.X;
                        character.Y = session.Y;
                        character.Z = session.Z;
                        character.Yaw = session.Yaw;
                    }
                    character.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not persist character position for player {PlayerId}", playerId);
            }

            await Clients.Others.SendAsync("PlayerLeft", playerId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private const float RadiansToDegrees = 57.2957795f;

    public sealed class MoveInputRequest
    {
        public float X { get; init; }
        public float Z { get; init; }
        public long Sequence { get; init; }
    }

    public sealed class PlayerSnapshot
    {
        public string PlayerId { get; init; } = "";
        public string Username { get; init; } = "";
        public int Level { get; init; }
        public string JobClass { get; init; } = "Novice";
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public float Yaw { get; init; }
    }

    private sealed class PlayerSession(
        string connectionId, string playerId, string username, int level, string jobClass,
        float x, float y, float z, float yaw)
    {
        public readonly object Sync = new();
        public string ConnectionId { get; } = connectionId;
        public string PlayerId { get; } = playerId;
        public string Username { get; } = username;
        public int Level { get; } = level;
        public string JobClass { get; } = jobClass;
        public float X = x;
        public float Y = y;
        public float Z = z;
        public float Yaw = yaw;
        public long LastSequence;
        public long LastInputTimestamp = Stopwatch.GetTimestamp();

        public PlayerSnapshot Snapshot() => new()
        {
            PlayerId = PlayerId, Username = Username, Level = Level, JobClass = JobClass,
            X = X, Y = Y, Z = Z, Yaw = Yaw
        };
    }
}
