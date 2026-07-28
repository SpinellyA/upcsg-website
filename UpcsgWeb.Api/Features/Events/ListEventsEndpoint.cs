using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class ListEventsEndpoint(IEventRepository events) : EndpointWithoutRequest<List<EventDto>>
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

        if (month is < 1 or > 12)
        {
            AddError("Month must be between 1 and 12.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var result = await events.GetForMonthAsync(year, month, ct);
        await Send.OkAsync([.. result.Select(e => e.ToDto())], ct);
    }
}
