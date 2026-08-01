using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Application.Features.Orders.GetMyOrders;

public class GetMyOrdersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetMyOrdersQuery, List<MyOrderListItem>>
{
    public async Task<List<MyOrderListItem>> Handle(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken) =>
        await context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == query.UserId)
            .OrderByDescending(o => o.PlacedAt)
            .Select(o => new MyOrderListItem(
                o.Id,
                o.PlacedAt,
                o.Status.ToString(),
                o.Lines.Sum(l => l.Quantity),

                // Summed in SQL rather than through Order.Total, which is a computed
                // property the provider cannot translate.
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.Lines
                    .Where(l => l.Status == OrderLineStatus.RefundDue)
                    .Sum(l => l.UnitPrice.Amount * l.Quantity)))
            .ToListAsync(cancellationToken);
}
