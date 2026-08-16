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
                // Loaded even when nothing will come back: whether stock moves depends
                // on the order's own status, which is the aggregate's call, not this
                // handler's to guess at.
                var stocked = await uow.Merch.GetManyAsync(
                    order.Lines.Select(l => l.MerchItemId), cancellationToken);

                order.Cancel(command.Reason ?? string.Empty, stocked.ToDictionary(i => i.Id));
                break;

            default:
                throw new DomainException(
                    $"{command.Status} is not a status an officer can set directly.");
        }

        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
