using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Settings;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class SiteSettingsRepository(UpcsgDbContext db)
    : Repository<SiteSettings>(db), ISiteSettingsRepository
{
    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        var settings = await Query.FirstOrDefaultAsync(ct);
        if (settings is not null)
        {
            return settings;
        }

        settings = SiteSettings.Create();
        Add(settings);
        await Db.SaveChangesAsync(ct);

        return settings;
    }
}
