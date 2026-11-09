using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrivateRagnarok.Api.Data;
using PrivateRagnarok.Api.Hubs;
using PrivateRagnarok.Api.Models;
using PrivateRagnarok.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Auth:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Auth:JwtKey must contain at least 32 bytes.");
var dbConnection = builder.Configuration.GetConnectionString("GameDb")
    ?? throw new InvalidOperationException("ConnectionStrings:GameDb is required.");
if (string.IsNullOrWhiteSpace(builder.Configuration["Game:InviteCode"]))
    throw new InvalidOperationException("Game:InviteCode is required.");

builder.Services.AddDbContext<GameDbContext>(o => o.UseNpgsql(dbConnection));
builder.Services.AddScoped<IPasswordHasher<PlayerAccount>, PasswordHasher<PlayerAccount>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddPolicy("UnityClient", p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Auth:Issuer"] ?? "PrivateRagnarok",
        ValidateAudience = true, ValidAudience = builder.Configuration["Auth:Audience"] ?? "PrivateRagnarokClient",
        ValidateIssuerSigningKey = true, IssuerSigningKey = key,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
    o.Events = new JwtBearerEvents { OnMessageReceived = c => {
        var token = c.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(token) && c.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            c.Token = token;
        return Task.CompletedTask;
    }};
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseCors("UnityClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapPost("/api/auth/register", async (RegisterRequest req, GameDbContext db,
    IPasswordHasher<PlayerAccount> hasher, TokenService tokens, IConfiguration config, CancellationToken ct) =>
{
    if (!string.Equals(req.InviteCode, config["Game:InviteCode"], StringComparison.Ordinal))
        return Results.BadRequest(new { error = "Invalid registration details." });
    var username = req.Username?.Trim();
    if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 24 ||
        !username.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        return Results.BadRequest(new { error = "Username must be 3-24 letters, numbers, or underscores." });
    if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length is < 12 or > 128)
        return Results.BadRequest(new { error = "Password must be 12-128 characters." });
    var normalized = username.ToUpperInvariant();
    if (await db.Players.AnyAsync(p => p.NormalizedUsername == normalized, ct))
        return Results.BadRequest(new { error = "Invalid registration details." });
    var player = new PlayerAccount { Username = username, NormalizedUsername = normalized, PasswordHash = string.Empty };
    player.PasswordHash = hasher.HashPassword(player, req.Password);
    db.Players.Add(player);
    try { await db.SaveChangesAsync(ct); }
    catch (DbUpdateException) { return Results.BadRequest(new { error = "Invalid registration details." }); }
    return Results.Ok(new AuthResponse(tokens.Create(player), player.Id, player.Username));
});

app.MapPost("/api/auth/login", async (LoginRequest req, GameDbContext db,
    IPasswordHasher<PlayerAccount> hasher, TokenService tokens, CancellationToken ct) =>
{
    var normalized = req.Username?.Trim().ToUpperInvariant() ?? "";
    var player = await db.Players.SingleOrDefaultAsync(p => p.NormalizedUsername == normalized, ct);
    if (player is null || hasher.VerifyHashedPassword(player, player.PasswordHash, req.Password ?? "")
        == PasswordVerificationResult.Failed) return Results.Unauthorized();
    player.LastLoginAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.Ok(new AuthResponse(tokens.Create(player), player.Id, player.Username));
});

app.MapGet("/api/me", (ClaimsPrincipal user) =>
    Results.Ok(new { playerId = user.FindFirstValue(ClaimTypes.NameIdentifier), username = user.Identity?.Name }))
    .RequireAuthorization();
app.MapHub<WorldHub>("/hubs/world").RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    await db.Database.EnsureCreatedAsync();

    // EnsureCreated does not evolve an existing database. Create the additive character
    // table explicitly so deployed installations made by earlier builds can upgrade safely.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "PlayerCharacters" (
            "PlayerId" uuid PRIMARY KEY REFERENCES "Players" ("Id") ON DELETE CASCADE,
            "CharacterName" character varying(24) NOT NULL,
            "JobClass" character varying(24) NOT NULL DEFAULT 'Novice',
            "Level" integer NOT NULL DEFAULT 1 CHECK ("Level" BETWEEN 1 AND 99),
            "BaseExperience" bigint NOT NULL DEFAULT 0 CHECK ("BaseExperience" >= 0),
            "X" real NOT NULL DEFAULT 0,
            "Y" real NOT NULL DEFAULT 0,
            "Z" real NOT NULL DEFAULT -7,
            "Yaw" real NOT NULL DEFAULT 0,
            "UpdatedAt" timestamp with time zone NOT NULL
        );
        """);
}
app.Run();

public partial class Program { }
