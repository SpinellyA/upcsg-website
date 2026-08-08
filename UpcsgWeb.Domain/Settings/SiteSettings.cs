using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Settings;

public class SiteSettings : AggregateRoot
{
    private SiteSettings() { }

    public int? EventsYear { get; private set; }

    public int? EventsMonth { get; private set; }

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static SiteSettings Create() => new() { Id = Guid.CreateVersion7() };

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

    public void FollowCurrentMonth()
    {
        EventsYear = null;
        EventsMonth = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private static readonly TimeSpan GuildOffset = TimeSpan.FromHours(8);

    public (int Year, int Month) ResolveEventsMonth() => ResolveEventsMonth(DateTime.UtcNow);

    public (int Year, int Month) ResolveEventsMonth(DateTime utcNow)
    {
        var now = utcNow + GuildOffset;
        return (EventsYear ?? now.Year, EventsMonth ?? now.Month);
    }
}
