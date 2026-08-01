using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetOrder;

/// <summary>
/// One order in full.
///
/// CallerIsOfficer is passed in rather than read here: the application layer has no
/// principal, and a handler that reached for one would be reaching into the transport.
/// </summary>
public record GetOrderQuery(Guid OrderId, Guid CallerId, bool CallerIsOfficer)
    : IQuery<OrderDto>;
