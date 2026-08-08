namespace UpcsgWeb.Shared.Contracts;

public static class UpcsgRoles
{
    public const string Member = "member";
    public const string Admin = "admin";
}

public class AppUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string Role { get; set; } = UpcsgRoles.Member;

    public bool IsAdmin => string.Equals(Role, UpcsgRoles.Admin, StringComparison.OrdinalIgnoreCase);
}

public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AppUserDto User { get; set; } = new();
}

public class GoogleTokenExchangeRequest
{
    public string Credential { get; set; } = string.Empty;
}
