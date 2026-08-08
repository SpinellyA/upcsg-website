using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.GetOrder;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

public class GetOrderEndpoint(ISender sender) : EndpointWithoutRequest<OrderDto>
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

        var order = await sender.Send(
            new GetOrderQuery(Route<Guid>("id"), userId.Value, User.IsAdmin()), ct);

        await Send.OkAsync(order, ct);
    }
}
