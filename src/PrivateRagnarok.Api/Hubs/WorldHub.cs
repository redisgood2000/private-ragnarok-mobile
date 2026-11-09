using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace PrivateRagnarok.Api.Hubs;

/// <summary>
/// Single-process authoritative movement prototype. Position is calculated on the server;
/// clients submit normalized input intent, never world coordinates.
/// </summary>
public sealed class WorldHub : Hub
{
    private static readonly ConcurrentDictionary<string, PlayerSession> Online = new();
    private const float WalkSpeed = 4.5f;
    private const float WorldHalfExtent = 28f;
    private const double MaxSimulationStepSeconds = 0.25;
    private const double MinimumInputIntervalSeconds = 0.05;

    public override async Task OnConnectedAsync()
    {
        var playerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = Context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(username))
        {
            Context.Abort();
            return;
        }

        var session = new PlayerSession(Context.ConnectionId, playerId, username, 0f, -7f);
        Online.AddOrUpdate(playerId, session, (_, _) => session);

        await Clients.Caller.SendAsync("PlayerSpawned", session.Snapshot());
        await Clients.Caller.SendAsync("OnlinePlayers",
            Online.Values.Where(x => x.PlayerId != playerId).Select(x => x.Snapshot()).ToArray());
        await Clients.Others.SendAsync("PlayerJoined", session.Snapshot());
        await base.OnConnectedAsync();
    }

    /// <summary>Submit movement intent. X/Z are direction inputs in [-1,1], not coordinates.</summary>
    public async Task MoveInput(MoveInputRequest request)
    {
        if (!float.IsFinite(request.X) || !float.IsFinite(request.Z))
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
                session.Yaw = MathF.Atan2(inputX, inputZ) * MathfRadiansToDegrees;
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
            await Clients.Others.SendAsync("PlayerLeft", playerId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private const float MathfRadiansToDegrees = 57.2957795f;

    public sealed record MoveInputRequest(float X, float Z, long Sequence);

    public sealed class PlayerSnapshot
    {
        public string PlayerId { get; init; } = "";
        public string Username { get; init; } = "";
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
        public float Yaw { get; init; }
    }

    private sealed class PlayerSession(
        string connectionId, string playerId, string username, float x, float z)
    {
        public readonly object Sync = new();
        public string ConnectionId { get; } = connectionId;
        public string PlayerId { get; } = playerId;
        public string Username { get; } = username;
        public float X = x;
        public float Y;
        public float Z = z;
        public float Yaw;
        public long LastSequence;
        public long LastInputTimestamp = Stopwatch.GetTimestamp();

        public PlayerSnapshot Snapshot() => new()
        {
            PlayerId = PlayerId, Username = Username,
            X = X, Y = Y, Z = Z, Yaw = Yaw
        };
    }
}
