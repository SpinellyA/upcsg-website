using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Application.Features.Orders.GetMyOrders;

public record GetMyOrdersQuery(Guid UserId) : IQuery<List<MyOrderListItem>>;

/// <summary>
/// Shaped for the list the guilder actually sees, not for the aggregate. The row needs a
/// total and an item count; loading Orders with their Lines to compute those in memory
/// would pull the whole history across the wire to render a table.
/// </summary>
public record MyOrderListItem(
    Guid Id,
    DateTime PlacedAt,
    string Status,
    int ItemCount,
    decimal Total,
    decimal RefundDue);
