using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Reads live data when an API is configured, otherwise serves the built-in sample so
/// the public site still renders standalone. Same pattern across the content services.
/// </summary>
public class EventService(HttpClient http, ApiOptions options) : IEventService
{
    private (int Year, int Month)? _cachedMonth;

    public async Task<(int Year, int Month)> GetDisplayMonthAsync()
    {
        if (_cachedMonth is not null)
        {
            return _cachedMonth.Value;
        }

        var now = DateTime.Now;

        if (!options.IsConfigured)
        {
            _cachedMonth = (now.Year, now.Month);
            return _cachedMonth.Value;
        }

        try
        {
            var settings = await http.GetFromJsonAsync<SiteSettingsDto>("api/settings", UpcsgJson.Options);
            _cachedMonth = settings is null
                ? (now.Year, now.Month)
                : (settings.ResolvedYear, settings.ResolvedMonth);
        }
        catch
        {
            // A settings hiccup shouldn't blank the events page.
            _cachedMonth = (now.Year, now.Month);
        }

        return _cachedMonth.Value;
    }

    public async Task<List<EventDto>> GetThisMonthEventsAsync()
    {
        if (options.IsConfigured)
        {
            var (year, month) = await GetDisplayMonthAsync();
            return await http.GetFromJsonAsync<List<EventDto>>($"api/events?year={year}&month={month}", UpcsgJson.Options) ?? [];
        }

        return SeedData();
    }

    private static List<EventDto> SeedData()
    {
        var now = DateTime.Now;
        return
        [
            new EventDto
            {
                Id = 1,
                Title = "Freshie Orientation Night",
                Description = "Welcome mixer for incoming CS freshies â€” org overview, games, and merch giveaways.",
                StartDateTime = new DateTime(now.Year, now.Month, 5, 17, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 5, 20, 0, 0),
                Location = "IT Park Auditorium, UP Cebu",
            },
            new EventDto
            {
                Id = 2,
                Title = "CodeSprint: Intro to Competitive Programming",
                Description = "Hands-on workshop covering the basics of algorithmic problem solving.",
                StartDateTime = new DateTime(now.Year, now.Month, 14, 13, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 14, 16, 0, 0),
                Location = "CS Laboratory 2",
            },
            new EventDto
            {
                Id = 3,
                Title = "General Assembly",
                Description = "Monthly org-wide meeting: updates from ExeCom, committee reports, and open forum.",
                StartDateTime = new DateTime(now.Year, now.Month, 22, 18, 0, 0),
                Location = "Online (Google Meet)",
            },
            new EventDto
            {
                Id = 4,
                Title = "Merch Drop: Cosmic Hoodie Pre-orders",
                Description = "Pre-order window opens for the 2026-2027 hoodie. Sizing samples available on site.",
                StartDateTime = new DateTime(now.Year, now.Month, 14, 17, 30, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 14, 19, 0, 0),
                Location = "CS Building Lobby",
            },
            new EventDto
            {
                Id = 5,
                Title = "Industry Night: Tech Talks",
                Description = "Alumni and industry partners share what the first year out of university actually looks like.",
                StartDateTime = new DateTime(now.Year, now.Month, 28, 18, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 28, 21, 0, 0),
                Location = "Innovation Hall, UP Cebu",
            },
        ];
    }
}
