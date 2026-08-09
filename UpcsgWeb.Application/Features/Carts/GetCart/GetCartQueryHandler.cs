using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Carts.GetCart;

public class GetCartQueryHandler(IUnitOfWork uow) : IQueryHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery query, CancellationToken cancellationToken)
    {
        var cart = await uow.Carts.GetForUserAsync(query.UserId, cancellationToken);

        if (cart is null)
        {
            return new CartDto();
        }

        var items = await uow.LoadPricingAsync(cart, cancellationToken);
        return cart.ToDto(items);
    }
}
