using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Officer sends a receipt back so the guilder can resubmit.</summary>
public class RejectReceiptEndpoint(IOrderRepository orders, IUnitOfWork uow)
    : Endpoint<RejectReceiptRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/receipt/reject");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Bounce a receipt back to AwaitingPayment with a reason.");
    }

    public override async Task HandleAsync(RejectReceiptRequest req, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(Route<Guid>("id"), ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            order.RejectReceipt(req.Reason);
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
