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

        // First run: materialise the row immediately rather than staging it, so a caller
        // that only reads settings still gets a persisted aggregate back.
        settings = SiteSettings.Create();
        Add(settings);
        await Db.SaveChangesAsync(ct);

        return settings;
    }
}
