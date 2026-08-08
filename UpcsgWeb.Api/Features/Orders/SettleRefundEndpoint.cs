using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.SettleRefund;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

public class SettleRefundEndpoint(ISender sender) : Endpoint<SettleRefundRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/settle-refund");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Record a refund that has been sent (officers only).");
    }

    public override async Task HandleAsync(SettleRefundRequest req, CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(new SettleRefundCommand(Route<Guid>("id"), req.Reference), ct), ct);
}
