using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetMyOrders;

public class GetMyOrdersQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetMyOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var orders = await uow.Orders.GetForUserAsync(query.UserId, cancellationToken);

        return [.. orders.Select(o => o.ToDto())];
    }
}
