using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

/// <summary>
/// Releases every confirmed order in one go, for the moment at a merch handover when a
/// queue of guilders collects at once and marking them off one at a time is the slowest
/// part of the table.
///
/// One request and one transaction rather than one call per order: a browser loop that
/// dies halfway leaves the officer unable to say which half went through.
/// </summary>
public class ReleaseConfirmedEndpoint(IOrderRepository orders, IUnitOfWork uow)
    : EndpointWithoutRequest<ReleaseConfirmedDto>
{
    public override void Configure()
    {
        Post("/orders/release-confirmed");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Mark every Acknowledged order as Released.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var confirmed = await orders.GetByStatusForUpdateAsync(OrderStatus.Acknowledged, ct);

        var released = new List<int>();
        var skipped = new List<string>();

        foreach (var order in confirmed)
        {
            try
            {
                order.Release();
                released.Add(order.Id);
            }
            catch (DomainException ex)
            {
                // One awkward order must not stop the rest of the queue. Say which, so
                // the officer can go and look rather than wonder.
                skipped.Add($"#{order.Id}: {ex.Message}");
            }
        }

        // Nothing was mutated if every order was skipped, but saving is harmless and
        // keeps the success path single.
        await uow.SaveChangesAsync(ct);

        await Send.OkAsync(new ReleaseConfirmedDto
        {
            ReleasedCount = released.Count,
            ReleasedOrderIds = released,
            Skipped = skipped,
        }, ct);
    }
}
