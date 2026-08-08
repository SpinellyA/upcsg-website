using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RefulfilLine;

public class RefulfilLineCommandHandler(IUnitOfWork uow)
    : ICommandHandler<RefulfilLineCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        RefulfilLineCommand command,
        CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        var items = await uow.Merch.GetManyAsync(
            order.Lines.Select(l => l.MerchItemId), cancellationToken);

        order.RefulfilLine(command.MerchItemId, command.Variant, items.ToDictionary(i => i.Id));

        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
