using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetMyOrders;

/// <summary>
/// A guilder's own order history, scoped by the token's user id and never by a
/// caller-supplied one.
///
/// This started as a narrow row projection, which reads better but is not what
/// /orders/mine actually serves: the page expands a row into its lines and receipt
/// without a second request, so the list has to carry whole orders.
/// </summary>
public record GetMyOrdersQuery(Guid UserId) : IQuery<List<OrderDto>>;
