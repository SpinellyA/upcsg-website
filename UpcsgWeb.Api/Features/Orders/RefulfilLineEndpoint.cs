using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.RefulfilLine;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>Fills a line that previously fell short, now that stock exists again.</summary>
public class RefulfilLineEndpoint(ISender sender) : Endpoint<RefulfilLineRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:guid}/refulfil");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Fill a short line after a restock (officers only).");
    }

    public override async Task HandleAsync(RefulfilLineRequest req, CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(
                new RefulfilLineCommand(Route<Guid>("id"), req.MerchItemId, req.Variant), ct),
            ct);
}
