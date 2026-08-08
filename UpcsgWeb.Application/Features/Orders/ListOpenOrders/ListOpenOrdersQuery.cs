using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ListOpenOrders;

public record ListOpenOrdersQuery(string? Status) : IQuery<List<OrderDto>>;
