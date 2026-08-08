using UpcsgWeb.Domain.Settings;

namespace UpcsgWeb.Application.Abstractions;

public interface ISiteSettingsRepository : IRepository<SiteSettings>
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
}
