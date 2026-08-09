using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.AddToCart;

public class AddToCartCommandHandler(IUnitOfWork uow) : ICommandHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand command, CancellationToken cancellationToken)
    {
        var item = await uow.Merch.GetByIdAsync(command.MerchItemId, cancellationToken)
            ?? throw new NotFoundException("That item");

        var cart = await uow.Carts.GetForUserAsync(command.UserId, cancellationToken);

        if (cart is null)
        {
            cart = Cart.Create(command.UserId);
            uow.Carts.Add(cart);
        }

        cart.AddItem(item, command.Variant, command.Quantity);

        await uow.SaveChangesAsync(cancellationToken);

        var items = await uow.LoadPricingAsync(cart, cancellationToken);
        return cart.ToDto(items);
    }
}
