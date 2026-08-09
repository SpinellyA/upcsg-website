using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Carts.ClearCart;

namespace UpcsgWeb.Api.Features.Carts;

public class ClearCartEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure() => Delete("/cart");

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await sender.Send(new ClearCartCommand(userId.Value), ct);
        await Send.NoContentAsync(ct);
    }
}
