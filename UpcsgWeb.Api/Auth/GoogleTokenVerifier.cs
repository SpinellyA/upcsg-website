using Google.Apis.Auth;
using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Api.Auth;

public class GoogleTokenVerifier(IConfiguration configuration, ILogger<GoogleTokenVerifier> logger)
    : IGoogleTokenVerifier
{
    public async Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Google:ClientId is not configured.");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId],
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            if (!payload.EmailVerified)
            {
                logger.LogWarning("Rejected Google sign-in: unverified email {Email}", payload.Email);
                return null;
            }

            return new GoogleIdentity(
                payload.Subject,
                payload.Email,
                payload.Name ?? payload.Email,
                payload.Picture,
                payload.HostedDomain);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Rejected Google sign-in: invalid token.");
            return null;
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Rejected Google sign-in: unparseable token.");
            return null;
        }
    }
}
