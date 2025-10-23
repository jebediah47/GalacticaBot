using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace GalacticaBot.Utils;

/// <summary>
/// Generates JWT tokens for authenticating with the GalacticaBot.Api SignalR hubs.
/// </summary>
public static class JwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT token for SignalR hub authentication.
    /// </summary>
    /// <param name="jwtSecret">The symmetric key (minimum 32 characters) from JWT_SECRET environment variable</param>
    /// <param name="botId">Optional bot ID to include in the token claims</param>
    /// <param name="expirationMinutes">Token expiration time in minutes (default: 60)</param>
    /// <returns>A valid JWT token as a string</returns>
    public static string GenerateToken(
        string jwtSecret,
        string? botId = null,
        int expirationMinutes = 60
    )
    {
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            throw new ArgumentException(
                "JWT secret must be at least 32 characters long.",
                nameof(jwtSecret)
            );
        }

        var key = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "bot-client"),
            new(
                JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        // Add optional bot_id claim
        if (!string.IsNullOrWhiteSpace(botId))
        {
            claims.Add(new Claim("bot_id", botId));
        }

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
