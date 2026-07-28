using Google.Apis.Auth;

namespace UpcsgWeb.Api.Auth;

/// <summary>
/// Validates the ID token against Google's published keys.
///
/// Verification is the whole security boundary here: without checking the signature and
/// audience, anyone could POST a hand-written JSON blob and be whoever they liked.
/// </summary>
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
                // Rejects tokens minted for a different app — without this, a token from
                // any other Google client would be accepted here.
                Audience = [clientId],
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            // Google sets this false for unverified addresses; treating those as identity
            // would let someone claim an address they don't control.
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
    }
}
