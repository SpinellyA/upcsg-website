using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.GetMyOrders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

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

        await Send.OkAsync(await sender.Send(new GetMyOrdersQuery(userId.Value), ct), ct);
    }
}
