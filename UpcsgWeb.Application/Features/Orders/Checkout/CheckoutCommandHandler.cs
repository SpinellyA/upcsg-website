using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.Checkout;

/// <summary>
/// Orchestration only: load what the domain needs, hand it over, save the result.
///
/// The rules themselves stay in <see cref="CheckoutService"/> — it spans Cart, Order and
/// MerchItem, so it belongs to the domain but to no single aggregate, and it has no
/// infrastructure in it. Rewriting it here would move domain rules into the application
/// layer and cost the ability to test checkout without a database.
/// </summary>
public class CheckoutCommandHandler(IUnitOfWork uow) : ICommandHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand command, CancellationToken cancellationToken)
    {
        var cart = await uow.Carts.GetForUserAsync(command.UserId, cancellationToken);

        if (cart is null || cart.IsEmpty)
        {
            throw new DomainException("Your cart is empty.");
        }

        // One round trip for every item in the cart, keyed by id: CheckoutService needs
        // the live MerchItem to snapshot its price and re-check stock.
        var items = await uow.Merch.GetManyAsync(
            cart.Lines.Select(l => l.MerchItemId), cancellationToken);

        var order = CheckoutService.Checkout(cart, items.ToDictionary(i => i.Id), command.Note);

        uow.Orders.Add(order);
        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
