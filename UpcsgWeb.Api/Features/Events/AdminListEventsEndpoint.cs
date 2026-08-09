using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class AdminListEventsEndpoint(ISender sender) : EndpointWithoutRequest<List<EventDto>>
{
    public override void Configure()
    {
        Get("/admin/events");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var year = Query<int?>("year", isRequired: false) ?? DateTime.UtcNow.Year;
        var month = Query<int?>("month", isRequired: false) ?? DateTime.UtcNow.Month;

        await Send.OkAsync(await sender.Send(new ListEventsForMonthQuery(year, month), ct), ct);
    }
}
