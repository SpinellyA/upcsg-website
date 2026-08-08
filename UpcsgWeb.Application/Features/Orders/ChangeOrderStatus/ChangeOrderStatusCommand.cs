using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ChangeOrderStatus;

public record ChangeOrderStatusCommand(
    Guid OrderId,
    OrderStatusDto Status,
    bool AllowShortfall,
    string? Reason) : ICommand<OrderDto>;
