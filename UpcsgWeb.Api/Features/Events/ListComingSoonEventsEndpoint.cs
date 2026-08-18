using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

// Separate from /events because these deliberately have no month to be listed under. The
// month endpoint answers "what is on in March"; this one answers "what is coming that we
// cannot date yet", and the events page shows the two as different sections.
public class ListComingSoonEventsEndpoint(ISender sender) : EndpointWithoutRequest<List<EventDto>>
{
    public override void Configure()
    {
        Get("/events/coming-soon");
        AllowAnonymous();
        Summary(s => s.Summary =
            "Announced events without a confirmed date, pencilled-in ones first.");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListComingSoonEventsQuery(), ct), ct);
}
