namespace UpcsgWeb.Domain.Users;

/// <summary>
/// Roles the API recognises. Assigned deliberately on the user row — completing a Google
/// sign-in proves identity and nothing more.
/// </summary>
public static class GuildRoles
{
    public const string Member = "member";
    public const string Admin = "admin";
}
