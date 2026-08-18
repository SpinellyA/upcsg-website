using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IEventService
{
    /// <summary>
    /// Confirmed events in the published month. Tentative ones are not included, because
    /// they have no day to sit on.
    /// </summary>
    Task<List<EventDto>> GetThisMonthEventsAsync();

    /// <summary>
    /// Announced events without a confirmed date, regardless of the published month.
    /// </summary>
    Task<List<EventDto>> GetComingSoonEventsAsync();

    Task<EventDto?> GetEventAsync(Guid id);

    Task<(int Year, int Month)> GetDisplayMonthAsync();

    void ForgetDisplayMonth();
}
