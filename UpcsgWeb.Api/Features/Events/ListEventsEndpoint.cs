using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class ListEventsEndpoint(ISender sender) : EndpointWithoutRequest<List<EventDto>>
{
    public override void Configure()
    {
        Get("/events");
        AllowAnonymous();
        Summary(s => s.Summary = "Events for a month (defaults to the current one).");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var year = Query<int?>("year", isRequired: false) ?? now.Year;
        var month = Query<int?>("month", isRequired: false) ?? now.Month;

        await Send.OkAsync(await sender.Send(new ListEventsForMonthQuery(year, month), ct), ct);
    }
}
