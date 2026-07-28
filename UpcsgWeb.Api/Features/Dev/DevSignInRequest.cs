using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Api.Features.Dev;

public class DevSignInRequest
{
    public string Role { get; set; } = GuildRoles.Member;
    public string? Email { get; set; }
}
