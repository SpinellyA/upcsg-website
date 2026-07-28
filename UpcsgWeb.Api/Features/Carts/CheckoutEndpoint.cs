using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;
using DomainOrder = UpcsgWeb.Domain.Orders.Order;

namespace UpcsgWeb.Api.Features.Carts;

/// <summary>
/// Turns the guilder's cart into an order awaiting payment.
///
/// Both the new order and the emptied cart are written by a single SaveChangesAsync —
/// that's the unit of work earning its keep. Committing them separately could leave an
/// order placed with the cart still full, and a refresh would order everything twice.
/// </summary>
public class CheckoutEndpoint(
    ICartRepository carts,
    IMerchRepository merch,
    IOrderRepository orders,
    IUnitOfWork uow)
    : Endpoint<CheckoutRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/cart/checkout");
        Summary(s => s.Summary = "Check out the cart. Creates an order awaiting a GCash receipt.");
    }

    public override async Task HandleAsync(CheckoutRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var cart = await carts.GetForUserAsync(userId.Value, ct);
        if (cart is null || cart.IsEmpty)
        {
            AddError("Your cart is empty.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var items = await CartOps.ResolveItemsAsync(cart, merch, ct);

        DomainOrder order;

        try
        {
            // Snapshots prices, re-validates availability, and clears the cart.
            order = CheckoutService.Checkout(cart, items, req.Note);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        orders.Add(order);
        await uow.SaveChangesAsync(ct);

        await Send.OkAsync(order.ToDto(), ct);
    }
}
