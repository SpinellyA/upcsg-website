using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Settings;

public class UpdateSettingsEndpoint(ISiteSettingsRepository settings, IUnitOfWork uow)
    : Endpoint<UpdateSiteSettingsRequest, SiteSettingsDto>
{
    public override void Configure()
    {
        Put("/settings");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Pin the events calendar to a month, or follow the real one.");
    }

    public override async Task HandleAsync(UpdateSiteSettingsRequest req, CancellationToken ct)
    {
        var current = await settings.GetAsync(ct);

        try
        {
            if (req.FollowCurrentMonth || req.EventsYear is null || req.EventsMonth is null)
            {
                current.FollowCurrentMonth();
            }
            else
            {
                current.ShowMonth(req.EventsYear.Value, req.EventsMonth.Value);
            }
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);

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
