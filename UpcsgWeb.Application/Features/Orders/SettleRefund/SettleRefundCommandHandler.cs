using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SettleRefund;

public class SettleRefundCommandHandler(IUnitOfWork uow)
    : ICommandHandler<SettleRefundCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        SettleRefundCommand command,
        CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        order.SettleRefund(command.Reference);
        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
