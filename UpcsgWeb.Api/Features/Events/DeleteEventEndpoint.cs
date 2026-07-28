using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Api.Features.Events;

public class DeleteEventEndpoint(IEventRepository events, IUnitOfWork uow) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/events/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var existing = await events.GetByIdAsync(Route<int>("id"), ct);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        events.Remove(existing);
        await uow.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
