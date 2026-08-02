using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAuthService
{
    /// <summary>Restores a persisted session, or null if there isn't a valid one.</summary>
    Task<AuthResultDto?> GetSessionAsync();

    /// <summary>
    /// Trades the ID token Google handed the browser for our own JWT. The API is what
    /// decides the user's role — signing in with Google proves identity, nothing more.
    /// </summary>
    Task<AuthResultDto> SignInWithGoogleAsync(string googleCredential);

    /// <summary>
    /// Stand-in used while no Google client id is configured. It calls the API's
    /// development sign-in endpoint, which is filtered out of the endpoint registry
    /// outside Development — so this cannot work against a deployed API.
    /// </summary>
    Task<AuthResultDto> SignInAsStubAsync(string role);

    Task SignOutAsync();
}
