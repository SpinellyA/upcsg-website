using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>
/// Moves an order along its lifecycle. The endpoint picks which method to call; the
/// aggregate decides whether the move is legal, so the rules live in exactly one place.
/// </summary>
public class ChangeOrderStatusEndpoint(IOrderRepository orders, IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<ChangeOrderStatusRequest, OrderDto>
{
    public override void Configure()
    {
        Patch("/orders/{id:guid}/status");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Advance or cancel an order (officers only).");
    }

    public override async Task HandleAsync(ChangeOrderStatusRequest req, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(Route<Guid>("id"), ct);
        if (order is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            switch (req.Status)
            {
                case OrderStatusDto.Acknowledged:
                    // Tracked entities: acknowledging is what deducts their stock, so the
                    // same unit of work has to save both the order and the merch.
                    var items = await merch.GetManyAsync(
                        order.Lines.Select(l => l.MerchItemId), ct);

                    var catalog = items.ToDictionary(i => i.Id);

                    if (req.AllowShortfall)
                    {
                        order.AcknowledgeWithShortfall(catalog);
                    }
                    else
                    {
                        order.Acknowledge(catalog);
                    }

                    break;
                case OrderStatusDto.Released:
                    order.Release();
                    break;
                case OrderStatusDto.Received:
                    order.MarkReceived();
                    break;
                case OrderStatusDto.Cancelled:
                    order.Cancel(req.Reason ?? string.Empty);
                    break;
                default:
                    // AwaitingPayment and Pending are reached by the guilder submitting
                    // or an officer rejecting a receipt, not by setting a status.
                    AddError($"{req.Status} is not a status an officer can set directly.");
                    await Send.ErrorsAsync(400, ct);
                    return;
            }
        }
        catch (DomainException ex)
        {
            // 409: the request is well-formed, it just conflicts with the order's
            // current state. A 500 here would misreport a client mistake as a bug.
            AddError(ex.Message);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(order.ToDto(), ct);
    }
}
