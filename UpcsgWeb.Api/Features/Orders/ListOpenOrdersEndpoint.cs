using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.ListOpenOrders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>The officer queue: everything not yet received or cancelled.</summary>
public class ListOpenOrdersEndpoint(ISender sender) : EndpointWithoutRequest<List<OrderDto>>
{
    public override void Configure()
    {
        Get("/orders");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(
                new ListOpenOrdersQuery(Query<string?>("status", isRequired: false)), ct),
            ct);
}
