using System.Security.Claims;

namespace UpcsgWeb.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id, taken from the validated JWT. Endpoints must use this
    /// rather than accepting a userId from the request body — otherwise any member
    /// could read or place orders as somebody else.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(sub, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(UpcsgWeb.Shared.Contracts.UpcsgRoles.Admin);
}
