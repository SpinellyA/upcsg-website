using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.Checkout;

/// <summary>
/// Turns the caller's cart into an order.
///
/// UserId comes from the authenticated principal at the edge, never from the request
/// body — a command that accepted it would let anyone check out as anyone.
///
/// Returns the whole order rather than its id: the client shows the confirmation screen
/// straight from this response, and returning an id would make every checkout two round
/// trips for no gain.
/// </summary>
public record CheckoutCommand(Guid UserId, string? Note) : ICommand<OrderDto>;
