using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetOrder;

public record GetOrderQuery(Guid OrderId, Guid CallerId, bool CallerIsOfficer)
    : IQuery<OrderDto>;
