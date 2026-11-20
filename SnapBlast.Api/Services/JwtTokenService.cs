using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public sealed class JwtTokenService(IOptions<AuthenticationOptions> options)
{
    public AuthResponse CreateToken(UserDocument user)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.JwtSecret) || Encoding.UTF8.GetByteCount(settings.JwtSecret) < 32)
        {
            throw new InvalidOperationException("Authentication:JwtSecret must be configured with at least 32 bytes.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        };
    }
}