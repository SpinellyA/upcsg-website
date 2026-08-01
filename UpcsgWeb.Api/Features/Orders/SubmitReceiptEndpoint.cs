using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.SubmitReceipt;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Guilder submits GCash proof, moving their order into the officers' queue.</summary>
public class SubmitReceiptEndpoint(ISender sender) : Endpoint<SubmitReceiptRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/receipt");
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

        var order = await sender.Send(
            new SubmitReceiptCommand(
                Route<Guid>("id"), userId.Value, req.ScreenshotUrl, req.ReferenceNumber),
            ct);

        await Send.OkAsync(order, ct);
    }
}
