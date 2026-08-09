using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Auth.GetCurrentUser;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Auth;

public class MeEndpoint(ISender sender) : EndpointWithoutRequest<AppUserDto>
{
    public override void Configure()
    {
        Get("/auth/me");
        Summary(s => s.Summary = "The signed-in guilder's own profile.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await sender.Send(new GetCurrentUserQuery(userId.Value), ct), ct);
    }
}
