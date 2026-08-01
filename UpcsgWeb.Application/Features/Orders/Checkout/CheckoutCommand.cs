using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Application.Features.Orders.Checkout;

/// <summary>
/// Turns the caller's cart into an order.
///
/// UserId comes from the authenticated principal at the edge, never from the request
/// body — a command that accepted it would let anyone check out as anyone.
/// </summary>
public record CheckoutCommand(Guid UserId, string? Note) : ICommand<Guid>;
