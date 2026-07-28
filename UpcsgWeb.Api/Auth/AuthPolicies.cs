namespace UpcsgWeb.Api.Auth;

public static class AuthPolicies
{
    /// <summary>Applied to every write endpoint. Membership alone is never enough.</summary>
    public const string ExeCom = "ExeCom";
}
