using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Carts;

public class AddToCartEndpoint(ICartRepository carts, IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<AddToCartRequest, CartDto>
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

        var item = await merch.GetByIdAsync(req.MerchItemId, ct);

        if (item is null)
        {
            AddError("That item does not exist.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        var cart = await CartOps.GetOrCreateAsync(carts, userId.Value, ct);

        try
        {
            // The aggregate re-checks stock, variant and the per-line cap against the
            // real item — the request only supplies an id and a quantity.
            cart.AddItem(item, req.Variant, req.Quantity);
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
