using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetMyOrders;

public record GetMyOrdersQuery(Guid UserId) : IQuery<List<OrderDto>>;
