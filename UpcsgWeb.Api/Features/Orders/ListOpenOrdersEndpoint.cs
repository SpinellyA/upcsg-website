using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>The officer queue: everything not yet received or cancelled.</summary>
public class ListOpenOrdersEndpoint(IOrderRepository orders) : EndpointWithoutRequest<List<OrderDto>>
{
    public override void Configure()
    {
        Get("/orders");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var status = Query<string?>("status", isRequired: false);

        // An unparseable status falls back to the open queue rather than erroring —
        // the filter is a convenience, not a contract.
        var result = Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed)
            ? await orders.GetByStatusAsync(parsed, ct)
            : await orders.GetOpenAsync(ct);

        await Send.OkAsync([.. result.Select(o => o.ToDto())], ct);
    }
}
