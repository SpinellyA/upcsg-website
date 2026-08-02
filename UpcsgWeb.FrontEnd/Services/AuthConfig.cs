namespace UpcsgWeb.FrontEnd.Services;

public static class AuthConfig
{
    // UseStubSignIn and GoogleClientId used to be constants here. They are now
    // GoogleAuthOptions, bound from wwwroot/appsettings.json: a WebAssembly app fetches
    // that file at startup, so a deployment can be pointed at a client id without a
    // rebuild — and the stand-in sign-in switches itself off the moment one is set,
    // rather than depending on somebody remembering to flip a bool before shipping.

    /// <summary>localStorage key holding the serialized session.</summary>
    public const string SessionStorageKey = "upcsg.session";
}
