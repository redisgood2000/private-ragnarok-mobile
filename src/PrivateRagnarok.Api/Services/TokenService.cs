using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PrivateRagnarok.Api.Models;
namespace PrivateRagnarok.Api.Services;
public sealed class TokenService(IConfiguration config)
{
    public string Create(PlayerAccount player)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Auth:JwtKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
            new Claim(ClaimTypes.Name, player.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: config["Auth:Issuer"] ?? "PrivateRagnarok",
            audience: config["Auth:Audience"] ?? "PrivateRagnarokClient",
            claims: claims, expires: DateTime.UtcNow.AddMinutes(20), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
