using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>
/// Fills a line that previously fell short, now that stock exists again.
///
/// Deliberately officer-initiated rather than something a restock does automatically: the
/// guilder may already have been told they are being refunded, and quietly resurrecting
/// their order after that conversation is worse than asking.
/// </summary>
public class RefulfilLineEndpoint(IOrderRepository orders, IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<RefulfilLineRequest, OrderDto>
{
    public override void Configure()
    {
        Post("/orders/{id:int}/refulfil");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Fill a short line after a restock (officers only).");
    }

    public override async Task HandleAsync(RefulfilLineRequest req, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(Route<int>("id"), ct);

        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Tracked, because filling the line takes the stock with it.
        var items = await merch.GetManyAsync(order.Lines.Select(l => l.MerchItemId), ct);

        try
        {
            order.RefulfilLine(req.MerchItemId, req.Variant, items.ToDictionary(i => i.Id));
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
