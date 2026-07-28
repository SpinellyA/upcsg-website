using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using UpcsgWeb.Shared.Contracts;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.FrontEnd.Auth;

/// <summary>
/// Auth state derived from the session our API issued.
///
/// This is a UI convenience only. Claims here come from localStorage, which the user can
/// edit â€” nothing on the client may be treated as a security boundary. Every privileged
/// action must be re-authorised by the API against the JWT signature.
/// </summary>
public class UpcsgAuthenticationStateProvider(IAuthService auth) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public AppUserDto? CurrentUser { get; private set; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var session = await auth.GetSessionAsync();
        if (session is null)
        {
            CurrentUser = null;
            return Anonymous;
        }

        CurrentUser = session.User;
        return new AuthenticationState(BuildPrincipal(session.User));
    }

    /// <summary>Called after a successful sign-in so the UI updates immediately.</summary>
    public void NotifySignedIn(AppUserDto user)
    {
        CurrentUser = user;
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(BuildPrincipal(user))));
    }

    public async Task SignOutAsync()
    {
        await auth.SignOutAsync();
        CurrentUser = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static ClaimsPrincipal BuildPrincipal(AppUserDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        if (!string.IsNullOrWhiteSpace(user.PictureUrl))
        {
            claims.Add(new Claim("picture", user.PictureUrl));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "upcsg"));
    }
}
