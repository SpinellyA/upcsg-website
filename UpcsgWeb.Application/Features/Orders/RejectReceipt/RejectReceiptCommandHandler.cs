using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RejectReceipt;

public class RejectReceiptCommandHandler(IUnitOfWork uow)
    : ICommandHandler<RejectReceiptCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        RejectReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        order.RejectReceipt(command.Reason);
        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
