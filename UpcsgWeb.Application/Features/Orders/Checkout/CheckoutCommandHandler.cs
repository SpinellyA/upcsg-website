using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.Checkout;

public class CheckoutCommandHandler(IUnitOfWork uow) : ICommandHandler<CheckoutCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CheckoutCommand command, CancellationToken cancellationToken)
    {
        var cart = await uow.Carts.GetForUserAsync(command.UserId, cancellationToken);

        if (cart is null || cart.IsEmpty)
        {
            throw new DomainException("Your cart is empty.");
        }

        var items = await uow.Merch.GetManyAsync(
            cart.Lines.Select(l => l.MerchItemId), cancellationToken);

        var order = CheckoutService.Checkout(cart, items.ToDictionary(i => i.Id), command.Note);

        uow.Orders.Add(order);
        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
