using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Single order. Officers see any; guilders see only their own.</summary>
public class GetOrderEndpoint(IOrderRepository orders) : EndpointWithoutRequest<OrderDto>
{
    public override void Configure() => Get("/orders/{id:guid}");

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var order = await orders.GetByIdAsync(Route<Guid>("id"), ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Ownership check: without it, any signed-in member could enumerate ids and
        // read everyone else's orders.
        if (order.UserId != userId.Value && !User.IsAdmin())
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        await Send.OkAsync(order.ToDto(), ct);
    }
}
