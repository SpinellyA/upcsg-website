namespace UpcsgWeb.Api.Auth;

public interface IGoogleTokenVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct);
}
