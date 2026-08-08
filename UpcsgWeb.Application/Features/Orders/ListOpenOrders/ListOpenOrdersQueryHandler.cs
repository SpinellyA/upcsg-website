using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ListOpenOrders;

public class ListOpenOrdersQueryHandler(IUnitOfWork uow)
    : IQueryHandler<ListOpenOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(
        ListOpenOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var orders = Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var parsed)
            ? await uow.Orders.GetByStatusAsync(parsed, cancellationToken)
            : await uow.Orders.GetOpenAsync(cancellationToken);

        var guilders = await uow.Users.GetByIdsAsync(
            orders.Select(o => o.UserId), cancellationToken);

        return [.. orders.Select(o => o.ToDto(guilders.GetValueOrDefault(o.UserId)))];
    }
}
