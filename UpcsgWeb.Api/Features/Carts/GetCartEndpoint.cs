using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Carts.GetCart;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class GetCartEndpoint(ISender sender) : EndpointWithoutRequest<CartDto>
{
    public override void Configure()
    {
        Get("/cart");
        Summary(s => s.Summary = "The signed-in guilder's cart, priced live.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await sender.Send(new GetCartQuery(userId.Value), ct), ct);
    }
}
