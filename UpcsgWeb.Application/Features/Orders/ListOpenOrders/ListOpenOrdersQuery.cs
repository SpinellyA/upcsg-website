using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ListOpenOrders;

/// <summary>
/// The officer queue. A null or unrecognised <paramref name="Status"/> means everything
/// still open — the filter is a convenience, not a contract, so a stale bookmark showing
/// the whole queue beats an error page.
/// </summary>
public record ListOpenOrdersQuery(string? Status) : IQuery<List<OrderDto>>;
