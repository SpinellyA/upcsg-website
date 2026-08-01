using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IEventService
{
    /// <summary>Events for the month the site is currently publishing.</summary>
    Task<List<EventDto>> GetThisMonthEventsAsync();

    /// <summary>
    /// A single event by id. Fetched directly rather than filtered out of the current
    /// month, so a shared link keeps working after the displayed month moves on.
    /// </summary>
    Task<EventDto?> GetEventAsync(Guid id);

    /// <summary>
    /// Which month that is. Officers can pin it ahead of the real calendar, so the
    /// events page must ask rather than assume DateTime.Now.
    /// </summary>
    Task<(int Year, int Month)> GetDisplayMonthAsync();

    /// <summary>
    /// Drops the cached month so the next read asks the API again.
    ///
    /// The answer is cached because otherwise every page that mentions events re-fetches
    /// settings — but in WebAssembly a scoped service lives as long as the tab. Without
    /// this, an officer who changes the month watches the site keep showing the old one
    /// until they hard-reload, which looks exactly like the setting failing to save.
    /// </summary>
    void ForgetDisplayMonth();
}
