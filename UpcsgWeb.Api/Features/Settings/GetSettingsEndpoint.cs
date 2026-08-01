using FastEndpoints;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Settings;

/// <summary>Public: the events page has to know which month to render.</summary>
public class GetSettingsEndpoint(ISiteSettingsRepository settings)
    : EndpointWithoutRequest<SiteSettingsDto>
{
    public override void Configure()
    {
        Get("/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);
        var (year, month) = current.ResolveEventsMonth();

        await Send.OkAsync(new SiteSettingsDto
        {
            EventsYear = current.EventsYear,
            EventsMonth = current.EventsMonth,
            ResolvedYear = year,
            ResolvedMonth = month,
        }, ct);
    }
}
