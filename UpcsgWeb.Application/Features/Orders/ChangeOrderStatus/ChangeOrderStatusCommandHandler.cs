using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ChangeOrderStatus;

public class ChangeOrderStatusCommandHandler(IUnitOfWork uow)
    : ICommandHandler<ChangeOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        ChangeOrderStatusCommand command,
        CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        switch (command.Status)
        {
            case OrderStatusDto.Acknowledged:
                // Tracked entities: acknowledging is what deducts their stock, so the
                // same unit of work has to save both the order and the merch.
                var items = await uow.Merch.GetManyAsync(
                    order.Lines.Select(l => l.MerchItemId), cancellationToken);

                var catalog = items.ToDictionary(i => i.Id);

                if (command.AllowShortfall)
                {
                    order.AcknowledgeWithShortfall(catalog);
                }
                else
                {
                    order.Acknowledge(catalog);
                }

                break;

            case OrderStatusDto.Released:
                order.Release();
                break;

            case OrderStatusDto.Received:
                order.MarkReceived();
                break;

            case OrderStatusDto.Cancelled:
                order.Cancel(command.Reason ?? string.Empty);
                break;

            default:
                // AwaitingPayment and Pending are reached by the guilder submitting or an
                // officer rejecting a receipt, not by setting a status.
                throw new DomainException(
                    $"{command.Status} is not a status an officer can set directly.");
        }

        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
