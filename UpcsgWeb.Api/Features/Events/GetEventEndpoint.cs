using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class GetEventEndpoint(IEventRepository events) : EndpointWithoutRequest<EventDto>
{
    public override void Configure()
    {
        Get("/events/{id:int}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await events.GetByIdAsync(Route<int>("id"), ct);
        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found.ToDto(), ct);
    }
}
