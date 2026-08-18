using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.Checkout;

public record CheckoutCommand(
    Guid UserId,
    string? Note,
    PaymentMethodDto PaymentMethod = PaymentMethodDto.GCash) : ICommand<OrderDto>;
