using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
namespace PrivateRagnarok.Api.Hubs;
public sealed class WorldHub : Hub
{
    private static readonly ConcurrentDictionary<string, PlayerPresence> Online = new();
    private const float MaxStep = 8f;
    private const float WorldLimit = 10000f;

    public override async Task OnConnectedAsync()
    {
        var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = Context.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) { Context.Abort(); return; }
        var p = new PlayerPresence(id, name, 0, 0, 0);
        Online[Context.ConnectionId] = p;
        await Clients.Others.SendAsync("PlayerJoined", p);
        await Clients.Caller.SendAsync("OnlinePlayers", Online.Values.Where(x => x.PlayerId != id));
        await base.OnConnectedAsync();
    }

    public async Task Move(MoveRequest request)
    {
        if (!float.IsFinite(request.X) || !float.IsFinite(request.Y) || !float.IsFinite(request.Z))
            throw new HubException("Invalid movement coordinates.");
        if (!Online.TryGetValue(Context.ConnectionId, out var old))
            throw new HubException("Player session not found.");
        var dx = request.X-old.X; var dy = request.Y-old.Y; var dz = request.Z-old.Z;
        if (dx*dx + dy*dy + dz*dz > MaxStep*MaxStep ||
            MathF.Abs(request.X)>WorldLimit || MathF.Abs(request.Y)>WorldLimit || MathF.Abs(request.Z)>WorldLimit)
            throw new HubException("Movement request rejected.");
        var next = old with { X=request.X, Y=request.Y, Z=request.Z };
        Online[Context.ConnectionId] = next;
        await Clients.Others.SendAsync("PlayerMoved", next);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Online.TryRemove(Context.ConnectionId, out var p))
            await Clients.Others.SendAsync("PlayerLeft", p.PlayerId);
        await base.OnDisconnectedAsync(exception);
    }
    public sealed record MoveRequest(float X, float Y, float Z);
    public sealed record PlayerPresence(string PlayerId, string Username, float X, float Y, float Z);
}
