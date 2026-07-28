using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Api.Features.Merch;

public class DeleteMerchEndpoint(IMerchRepository merch, IUnitOfWork uow) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/merch/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var item = await merch.GetByIdAsync(Route<int>("id"), ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Safe because order lines carry snapshots and hold no FK to this row, so
        // history survives the item being discontinued.
        merch.Remove(item);
        await uow.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
