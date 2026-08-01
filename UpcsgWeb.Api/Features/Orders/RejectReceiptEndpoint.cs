using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.RejectReceipt;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Officer sends a receipt back so the guilder can resubmit.</summary>
public class RejectReceiptEndpoint(ISender sender) : Endpoint<RejectReceiptRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/receipt/reject");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Bounce a receipt back to AwaitingPayment with a reason.");
    }

    public override async Task HandleAsync(RejectReceiptRequest req, CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(new RejectReceiptCommand(Route<Guid>("id"), req.Reason), ct), ct);
}
