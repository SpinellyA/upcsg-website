using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ChangeOrderStatus;

/// <summary>
/// Moves an order along its lifecycle. The handler picks which aggregate method to call;
/// the aggregate decides whether the move is legal, so the rules stay in one place.
/// </summary>
public record ChangeOrderStatusCommand(
    Guid OrderId,
    OrderStatusDto Status,
    bool AllowShortfall,
    string? Reason) : ICommand<OrderDto>;
