namespace UpcsgWeb.FrontEnd.Services;

public static class AuthConfig
{
    /// <summary>
    /// True until the API's token-exchange endpoint and a Google client ID exist.
    /// While set, the login page offers stub sign-in so the authenticated UI can be
    /// exercised locally. Flip to false and the real Google flow takes over.
    /// </summary>
    public const bool UseStubSignIn = true;

    /// <summary>
    /// From Google Cloud Console â†’ Credentials â†’ OAuth client ID (Web application).
    /// Authorised origins and redirect URIs must list both localhost and the production
    /// GitHub Pages URL, or one of the two environments will fail.
    /// </summary>
    public const string GoogleClientId = "";

    /// <summary>localStorage key holding the serialized session.</summary>
    public const string SessionStorageKey = "upcsg.session";
}
