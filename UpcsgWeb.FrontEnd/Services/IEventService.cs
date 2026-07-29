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
    Task<EventDto?> GetEventAsync(int id);

    /// <summary>
    /// Which month that is. Officers can pin it ahead of the real calendar, so the
    /// events page must ask rather than assume DateTime.Now.
    /// </summary>
    Task<(int Year, int Month)> GetDisplayMonthAsync();
}
