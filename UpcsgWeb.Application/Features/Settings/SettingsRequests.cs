using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Settings;

public record GetSettingsQuery : IQuery<SiteSettingsDto>;

public record UpdateSettingsCommand(UpdateSiteSettingsRequest Settings) : ICommand<SiteSettingsDto>;

public class GetSettingsQueryHandler(IUnitOfWork uow) : IQueryHandler<GetSettingsQuery, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(GetSettingsQuery query, CancellationToken ct)
    {
        var current = await uow.SiteSettings.GetAsync(ct);
        return current.ToDto();
    }
}

public class UpdateSettingsCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateSettingsCommand, SiteSettingsDto>
{
    public async Task<SiteSettingsDto> Handle(UpdateSettingsCommand command, CancellationToken ct)
    {
        var current = await uow.SiteSettings.GetAsync(ct);
        var req = command.Settings;

        if (req.FollowCurrentMonth || req.EventsYear is null || req.EventsMonth is null)
        {
            current.FollowCurrentMonth();
        }
        else
        {
            current.ShowMonth(req.EventsYear.Value, req.EventsMonth.Value);
        }

        await uow.SaveChangesAsync(ct);

        return current.ToDto();
    }
}
