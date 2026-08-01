using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Api.Features.Carts;

public class ClearCartEndpoint(ICartRepository carts, IUnitOfWork uow) : EndpointWithoutRequest
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

        var cart = await carts.GetForUserAsync(userId.Value, ct);
        if (cart is not null)
        {
            cart.Clear();
            await uow.SaveChangesAsync(ct);
        }

        // Idempotent: clearing an absent cart is a success, not a 404.
        await Send.NoContentAsync(ct);
    }
}
