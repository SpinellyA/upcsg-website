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

    public void ForgetDisplayMonth() => _cachedMonth = null;

    public async Task<List<EventDto>> GetThisMonthEventsAsync()
    {
        if (options.IsConfigured)
        {
            var (year, month) = await GetDisplayMonthAsync();
            return await http.GetFromJsonAsync<List<EventDto>>($"api/events?year={year}&month={month}", UpcsgJson.Options) ?? [];
        }

        return SeedData();
    }

    public async Task<EventDto?> GetEventAsync(Guid id)
    {
        if (!options.IsConfigured)
        {
            return SeedData().FirstOrDefault(e => e.Id == id);
        }

        // 404 is an ordinary answer here — a bad id in the URL — so it must not surface
        // as an exception the page has to catch.
        var response = await http.GetAsync($"api/events/{id}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EventDto>(UpcsgJson.Options);
    }

    private static List<EventDto> SeedData()
    {
        var now = DateTime.Now;
        return
        [
            // Descriptions are multi-paragraph on purpose: the detail page splits on blank
            // lines, and a one-liner leaves it looking like a stub.
            new EventDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000001"),
                Title = "Freshie Orientation Night",
                Description =
                    """
                    Your first night as part of the guild. We run through what UPCSG actually does
                    across a school year — the academic support, the competitions we send teams to,
                    the socials, and the committees you can join — then get out of the way so you
                    can meet the people you'll be spending the next four years with.

                    The second half is games and giveaways. Bring nothing but yourself; we'll handle
                    the rest. Merch samples will be out on a table near the entrance if you want to
                    check sizing before the pre-order window opens later this month.

                    Open to all incoming Computer Science freshies. Upperclassmen are welcome to
                    drop in, and honestly we'd rather you did — the orientation goes better when
                    there are people around who've already been through it.
                    """,
                StartDateTime = new DateTime(now.Year, now.Month, 5, 17, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 5, 20, 0, 0),
                Location = "IT Park Auditorium, UP Cebu",
            },
            new EventDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000002"),
                Title = "CodeSprint: Intro to Competitive Programming",
                Description =
                    """
                    A hands-on introduction to algorithmic problem solving, aimed at people who have
                    never touched a contest problem before. We start from reading a problem statement
                    properly and work up through complexity, greedy reasoning, and binary search.

                    Bring a laptop. Any language with a working compiler or interpreter is fine —
                    the ideas transfer, and nobody is going to tell you your choice of language is
                    wrong.

                    No prerequisites beyond an introductory programming course. If you can write a
                    loop and an array, you have enough to follow along.
                    """,
                StartDateTime = new DateTime(now.Year, now.Month, 14, 13, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 14, 16, 0, 0),
                Location = "CS Laboratory 2",
            },
            new EventDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000003"),
                Title = "General Assembly",
                Description =
                    """
                    The monthly org-wide meeting. ExeCom reports on what's moved since the last GA,
                    each committee gives a short update, and then the floor opens.

                    The open forum is the part that matters. If you have a question about how
                    something is being run, where the funds went, or why a decision was made, this
                    is the venue for it — and it's on the record.
                    """,
                StartDateTime = new DateTime(now.Year, now.Month, 22, 18, 0, 0),
                Location = "Online (Google Meet)",
            },
            new EventDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000004"),
                Title = "Merch Drop: Cosmic Hoodie Pre-orders",
                Description =
                    """
                    The pre-order window for the 2026–2027 Cosmic Hoodie opens. Sizing samples will
                    be on site, so come try one on before you commit to a size.

                    Pre-orders go through the guild store on this site: add the hoodie to your cart,
                    check out, then send your GCash reference. An officer verifies the payment and
                    your order moves to confirmed.
                    """,
                StartDateTime = new DateTime(now.Year, now.Month, 14, 17, 30, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 14, 19, 0, 0),
                Location = "CS Building Lobby",
            },
            new EventDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000005"),
                Title = "Industry Night: Tech Talks",
                Description =
                    """
                    Alumni and industry partners talk about what the first year out of university
                    actually looks like — the parts nobody puts in a job posting.

                    Expect frank answers on interviews, what a junior role really involves day to
                    day, and which of the things you're learning now turn out to matter. Bring
                    questions; the Q&A usually runs longer than the talks.
                    """,
                StartDateTime = new DateTime(now.Year, now.Month, 28, 18, 0, 0),
                EndDateTime = new DateTime(now.Year, now.Month, 28, 21, 0, 0),
                Location = "Innovation Hall, UP Cebu",
            },
        ];
    }
}
