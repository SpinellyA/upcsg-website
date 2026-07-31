using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Settings;

/// <summary>
/// Site-wide switches an officer can flip. A single row.
///
/// Exists mainly so the events calendar isn't hardwired to DateTime.Now: the guild
/// plans a month ahead, and an officer needs to publish next month before it starts
/// without editing code.
/// </summary>
public class SiteSettings : AggregateRoot
{
    private SiteSettings() { } // EF

    /// <summary>Null means "follow the real calendar", which is the normal case.</summary>
    public int? EventsYear { get; private set; }

    public int? EventsMonth { get; private set; }

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static SiteSettings CreateDefault() => new();

    /// <summary>Pins the events page to a specific month.</summary>
    public void ShowMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new DomainException("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > DateTime.UtcNow.Year + 5)
        {
            throw new DomainException($"Year {year} is out of range.");
        }

        EventsYear = year;
        EventsMonth = month;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Back to tracking the real calendar.</summary>
    public void FollowCurrentMonth()
    {
        EventsYear = null;
        EventsMonth = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The guild is in Cebu: UTC+8, and the Philippines has no daylight saving, so a
    /// fixed offset is exact rather than an approximation.
    ///
    /// This matters at the turn of a month. Read in UTC, the calendar still says July
    /// for the first eight hours of August in Cebu — the site would show last month's
    /// events on the morning of the first, which an officer would reasonably read as
    /// the setting having failed to save.
    /// </summary>
    private static readonly TimeSpan GuildOffset = TimeSpan.FromHours(8);

    /// <summary>The month the public events page should render.</summary>
    public (int Year, int Month) ResolveEventsMonth() => ResolveEventsMonth(DateTime.UtcNow);

    /// <summary>Testable overload: the caller supplies "now" as a UTC instant.</summary>
    public (int Year, int Month) ResolveEventsMonth(DateTime utcNow)
    {
        var now = utcNow + GuildOffset;
        return (EventsYear ?? now.Year, EventsMonth ?? now.Month);
    }
}
