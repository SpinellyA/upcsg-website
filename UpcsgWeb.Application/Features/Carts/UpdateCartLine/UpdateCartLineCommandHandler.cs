using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.UpdateCartLine;

public class UpdateCartLineCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateCartLineCommand, CartDto>
{
    public async Task<CartDto> Handle(
        UpdateCartLineCommand command,
        CancellationToken cancellationToken)
    {
        var cart = await uow.Carts.GetForUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("Your cart");

        // Loaded so the quantity can be checked against stock as it stands now. Removing a
        // line is exempt: an item that has been withdrawn entirely cannot be looked up, and
        // refusing to remove it would leave the guilder stuck with a cart they cannot clear.
        if (command.Quantity == 0)
        {
            cart.RemoveItem(command.MerchItemId, command.Variant);
        }
        else
        {
            var item = await uow.Merch.GetByIdAsync(command.MerchItemId, cancellationToken)
                ?? throw new NotFoundException("That item");

            cart.SetQuantity(item, command.Variant, command.Quantity);
        }

        await uow.SaveChangesAsync(cancellationToken);

        var items = await uow.LoadPricingAsync(cart, cancellationToken);
        return cart.ToDto(items);
    }
}
