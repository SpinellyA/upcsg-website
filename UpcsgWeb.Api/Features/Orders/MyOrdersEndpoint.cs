using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

// Orders are created only by POST /cart/checkout. There is deliberately no endpoint
// that accepts a list of lines directly — that would be a second way to build an order
// and a second place for the pricing rules to drift.

/// <summary>A guilder's own order history.</summary>
public class MyOrdersEndpoint(IOrderRepository orders) : EndpointWithoutRequest<List<OrderDto>>
{
    public override void Configure() => Get("/orders/mine");

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Scoped by the token's user id, never by a caller-supplied one.
        var result = await orders.GetForUserAsync(userId.Value, ct);
        await Send.OkAsync([.. result.Select(o => o.ToDto())], ct);
    }
}
