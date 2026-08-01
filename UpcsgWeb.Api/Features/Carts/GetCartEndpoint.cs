using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class GetCartEndpoint(ICartRepository carts, IMerchRepository merch)
    : EndpointWithoutRequest<CartDto>
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

        var cart = await CartOps.GetOrCreateAsync(carts, userId.Value, ct);
        var items = await CartOps.ResolveItemsAsync(cart, merch, ct);

        await Send.OkAsync(cart.ToDto(items), ct);
    }
}
