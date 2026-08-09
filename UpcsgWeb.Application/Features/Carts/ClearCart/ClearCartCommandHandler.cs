using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Application.Features.Carts.ClearCart;

public class ClearCartCommandHandler(IUnitOfWork uow) : ICommandHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await uow.Carts.GetForUserAsync(command.UserId, cancellationToken);

        if (cart is null)
        {
            return;
        }

        cart.Clear();
        await uow.SaveChangesAsync(cancellationToken);
    }
}
