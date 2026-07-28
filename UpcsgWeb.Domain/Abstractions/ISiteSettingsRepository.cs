using UpcsgWeb.Domain.Settings;

namespace UpcsgWeb.Domain.Abstractions;

public interface ISiteSettingsRepository : IRepository<SiteSettings>
{
    /// <summary>
    /// Settings are a single row, so callers ask for "the" settings rather than an id.
    /// Creates the row on first access so no caller ever handles a null.
    ///
    /// The inherited CRUD is honest but mostly unused here — a singleton aggregate is
    /// the one case where the generic surface is wider than the aggregate needs.
    /// </summary>
    Task<SiteSettings> GetAsync(CancellationToken ct = default);
}
