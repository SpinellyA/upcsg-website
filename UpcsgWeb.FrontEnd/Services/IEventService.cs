using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IEventService
{
    Task<List<EventDto>> GetThisMonthEventsAsync();

    Task<EventDto?> GetEventAsync(Guid id);

    Task<(int Year, int Month)> GetDisplayMonthAsync();

    void ForgetDisplayMonth();
}
