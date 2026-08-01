using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.ChangeOrderStatus;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>
/// Moves an order along its lifecycle. The handler picks which method to call; the
/// aggregate decides whether the move is legal, so the rules live in exactly one place.
/// </summary>
public class ChangeOrderStatusEndpoint(ISender sender) : Endpoint<ChangeOrderStatusRequest, OrderDto>
{
    public override void Configure()
    {
        Patch("/orders/{id:guid}/status");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Advance or cancel an order (officers only).");
    }

    public override async Task HandleAsync(ChangeOrderStatusRequest req, CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(
                new ChangeOrderStatusCommand(
                    Route<Guid>("id"), req.Status, req.AllowShortfall, req.Reason),
                ct),
            ct);
}
