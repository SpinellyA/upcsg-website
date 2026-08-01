using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class UpdateCartLineEndpoint(ICartRepository carts, IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<UpdateCartLineRequest, CartDto>
{
    public override void Configure()
    {
        Patch("/cart/items");
        Summary(s => s.Summary = "Set an absolute quantity; zero removes the line.");
    }

    public override async Task HandleAsync(UpdateCartLineRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var cart = await carts.GetForUserAsync(userId.Value, ct);
        if (cart is null)
        {
            AddError("Your cart is empty.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        try
        {
            cart.SetQuantity(req.MerchItemId, req.Variant, req.Quantity);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);

        var items = await CartOps.ResolveItemsAsync(cart, merch, ct);
        await Send.OkAsync(cart.ToDto(items), ct);
    }
}
