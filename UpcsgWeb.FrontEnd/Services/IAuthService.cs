using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAuthService
{
    /// <summary>Restores a persisted session, or null if there isn't a valid one.</summary>
    Task<AuthResultDto?> GetSessionAsync();

    /// <summary>
    /// Trades the ID token Google handed the browser for our own JWT. The API is what
    /// decides the user's role â€” signing in with Google proves identity, nothing more.
    /// </summary>
    Task<AuthResultDto> SignInWithGoogleAsync(string googleCredential);

    /// <summary>
    /// Stand-in used while there is no Google client ID and no API to exchange against.
    /// Delete this together with <see cref="AuthConfig.UseStubSignIn"/> once auth is live.
    /// </summary>
    Task<AuthResultDto> SignInAsStubAsync(string role);

    Task SignOutAsync();
}
