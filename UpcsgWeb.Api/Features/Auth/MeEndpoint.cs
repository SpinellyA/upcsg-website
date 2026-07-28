using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Auth;

/// <summary>
/// Lets the client confirm who the API thinks it is talking to — the authoritative
/// answer, as opposed to the claims cached in the browser.
/// </summary>
public class MeEndpoint(IUserRepository users) : EndpointWithoutRequest<AppUserDto>
{
    public override void Configure() => Get("/auth/me");

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await users.GetByIdAsync(userId.Value, ct);
        if (user is null)
        {
            // Token is valid but the row is gone — treat as signed out.
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(user.ToDto(), ct);
    }
}
