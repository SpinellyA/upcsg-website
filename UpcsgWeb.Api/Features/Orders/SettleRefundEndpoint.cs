using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>
/// Records that money owed on an order actually went back, with the GCash reference.
///
/// The transfer itself happens in GCash — there is no payment API here. What this endpoint
/// does is make the transfer auditable: without it, a partial refund is an officer's
/// private act that the Treasurer cannot reconcile and the next ExeCom cannot explain.
/// </summary>
public class SettleRefundEndpoint(IOrderRepository orders, IUnitOfWork uow)
    : Endpoint<SettleRefundRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/settle-refund");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Record a refund that has been sent (officers only).");
    }

    public override async Task HandleAsync(SettleRefundRequest req, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(Route<Guid>("id"), ct);

        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            order.SettleRefund(req.Reference);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(order.ToDto(), ct);
    }
}
