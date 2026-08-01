namespace UpcsgWeb.Shared.Contracts;

/// <summary>Roles are assigned in our own database, never inferred from the Google sign-in.</summary>
public static class UpcsgRoles
{
    public const string Member = "member";
    public const string Admin = "admin";
}

/// <summary>
/// The signed-in user as our API sees them. Deliberately excludes date of birth:
/// Google's OIDC profile scope does not return it, and the People API route needs a
/// sensitive-scope review for a field most accounts leave unset or partial.
/// </summary>
public class AppUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string Role { get; set; } = UpcsgRoles.Member;

    public bool IsAdmin => string.Equals(Role, UpcsgRoles.Admin, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// What the API returns from the token exchange: our own JWT plus the resolved user.
/// The Google credential is never stored — it is traded for this and discarded.
/// </summary>
public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AppUserDto User { get; set; } = new();
}

/// <summary>Request body for the exchange: the ID token Google handed the browser.</summary>
public class GoogleTokenExchangeRequest
{
    public string Credential { get; set; } = string.Empty;
}

