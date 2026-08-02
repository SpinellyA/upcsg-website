namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// The Google OAuth client id, read from wwwroot/appsettings.json at startup.
///
/// Runtime configuration rather than the compile-time constant this used to be. A
/// WebAssembly app fetches appsettings.json on load, so the deployed site can be pointed
/// at a different client id by editing one file — where a const would mean rebuilding
/// and republishing the whole app to change a public identifier.
///
/// The client id is not a secret. It is sent to every browser that loads the sign-in
/// page. What protects the flow is the authorised-origins list on the Google credential
/// and the API verifying the returned token server-side.
/// </summary>
public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
