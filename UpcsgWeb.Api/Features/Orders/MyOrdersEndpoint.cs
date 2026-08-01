using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.GetMyOrders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

// Orders are created only by POST /cart/checkout. There is deliberately no endpoint
// that accepts a list of lines directly — that would be a second way to build an order
// and a second place for the pricing rules to drift.

/// <summary>A guilder's own order history.</summary>
public class MyOrdersEndpoint(ISender sender) : EndpointWithoutRequest<List<OrderDto>>
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
        await Send.OkAsync(await sender.Send(new GetMyOrdersQuery(userId.Value), ct), ct);
    }
}
