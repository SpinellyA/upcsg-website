using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Guilder submits GCash proof, moving their order into the officers' queue.</summary>
public class SubmitReceiptEndpoint(IOrderRepository orders, IUnitOfWork uow)
    : Endpoint<SubmitReceiptRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:int}/receipt");
        Summary(s => s.Summary = "Attach a GCash receipt to an order awaiting payment.");
    }

    public override async Task HandleAsync(SubmitReceiptRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var order = await orders.GetByIdAsync(Route<int>("id"), ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Paying is the guilder's own act. Officers can move statuses but must not be
        // able to fabricate a receipt on someone's behalf — so no admin bypass here.
        if (order.UserId != userId.Value)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var receipt = PaymentReceipt.FromScreenshot(req.ScreenshotUrl, req.ReferenceNumber);
            order.SubmitReceipt(receipt);
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
