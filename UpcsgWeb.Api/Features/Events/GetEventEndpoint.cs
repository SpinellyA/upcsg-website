using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class GetEventEndpoint(ISender sender) : EndpointWithoutRequest<EventDto>
{
    public override void Configure()
    {
        Get("/events/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await sender.Send(new GetEventQuery(Route<Guid>("id")), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found, ct);
    }
}
