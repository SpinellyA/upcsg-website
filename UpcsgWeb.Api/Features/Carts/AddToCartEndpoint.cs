using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Carts.AddToCart;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class AddToCartEndpoint(ISender sender) : Endpoint<AddToCartRequest, CartDto>
{
    public override void Configure() => Post("/cart/items");

    public override async Task HandleAsync(AddToCartRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var cart = await sender.Send(
            new AddToCartCommand(userId.Value, req.MerchItemId, req.Variant, req.Quantity), ct);

        await Send.OkAsync(cart, ct);
    }
}
