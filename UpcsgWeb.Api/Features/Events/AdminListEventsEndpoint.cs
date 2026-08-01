using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

/// <summary>
/// Month browsing for the CMS, so officers can edit past and future months — not just
/// whichever one the public page happens to be pinned to.
/// </summary>
public class AdminListEventsEndpoint(IEventRepository events) : EndpointWithoutRequest<List<EventDto>>
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

        var result = await events.GetForMonthAsync(year, month, ct);
        await Send.OkAsync([.. result.Select(e => e.ToDto())], ct);
    }
}
