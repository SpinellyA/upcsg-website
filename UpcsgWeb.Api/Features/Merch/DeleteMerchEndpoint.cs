using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Api.Features.Merch;

public class DeleteMerchEndpoint(IMerchRepository merch, IUnitOfWork uow) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/merch/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var item = await merch.GetByIdAsync(Route<Guid>("id"), ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        merch.Remove(item);
        await uow.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
