using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UpcsgWeb.Api.Auth;

/// <summary>
/// Mints the API's own JWT. Google proves who someone is; this token says what they may
/// do, which is why the role argument comes from our own user row.
///
/// Takes primitives rather than the AppUser aggregate: token shape is a delivery
/// concern, and the domain shouldn't acquire a dependency on it.
/// </summary>
public class JwtIssuer(IConfiguration configuration)
{
    public const string Issuer = "upcsg-api";
    public const string Audience = "upcsg-web";

    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public (string Token, DateTimeOffset ExpiresAt) Issue(
        int userId, string email, string name, string role, string? pictureUrl)
    {
        var key = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var expiresAt = DateTimeOffset.UtcNow.Add(Lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role),
        };

        if (!string.IsNullOrWhiteSpace(pictureUrl))
        {
            claims.Add(new Claim("picture", pictureUrl));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
