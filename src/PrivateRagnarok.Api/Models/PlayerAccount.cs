namespace PrivateRagnarok.Api.Models;
public sealed class PlayerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Username { get; set; }
    public required string NormalizedUsername { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}
public sealed record RegisterRequest(string? Username, string? Password, string? InviteCode);
public sealed record LoginRequest(string? Username, string? Password);
public sealed record AuthResponse(string AccessToken, Guid PlayerId, string Username);
