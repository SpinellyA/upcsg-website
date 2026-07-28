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

    /// <summary>The month the public events page should render.</summary>
    public (int Year, int Month) ResolveEventsMonth()
    {
        var now = DateTime.UtcNow;
        return (EventsYear ?? now.Year, EventsMonth ?? now.Month);
    }
}
