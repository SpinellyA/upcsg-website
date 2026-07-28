namespace UpcsgWeb.Api.Auth;

public interface IGoogleTokenVerifier
{
    /// <summary>Returns null when the token fails validation, for any reason.</summary>
    Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct);
}
