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

        cart.SetQuantity(command.MerchItemId, command.Variant, command.Quantity);

        await uow.SaveChangesAsync(cancellationToken);

        var items = await uow.LoadPricingAsync(cart, cancellationToken);
        return cart.ToDto(items);
    }
}
