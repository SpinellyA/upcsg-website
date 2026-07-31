using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Settings;

namespace UpcsgWeb.Domain.Tests;

public class SiteSettingsTests
{
    // 4:30 PM UTC on 31 July is already half past midnight on 1 August in Cebu.
    private static readonly DateTime LateJulyUtc = new(2026, 7, 31, 16, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Following_the_calendar_uses_guild_local_time_not_utc()
    {
        var settings = SiteSettings.CreateDefault();

        var (year, month) = settings.ResolveEventsMonth(LateJulyUtc);

        // The officer opening the site in Cebu sees August, because for them it is
        // August. Reading this in UTC is how the first morning of a month ends up
        // showing the previous month's events.
        Assert.Equal(2026, year);
        Assert.Equal(8, month);
    }

    [Fact]
    public void Following_the_calendar_rolls_the_year_over_too()
    {
        var settings = SiteSettings.CreateDefault();

        // 5 PM UTC on 31 December is 1 January in Cebu.
        var (year, month) = settings.ResolveEventsMonth(new DateTime(2026, 12, 31, 17, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2027, year);
        Assert.Equal(1, month);
    }

    [Fact]
    public void A_pinned_month_ignores_the_clock_entirely()
    {
        var settings = SiteSettings.CreateDefault();
        settings.ShowMonth(2026, 7);

        var (year, month) = settings.ResolveEventsMonth(LateJulyUtc);

        Assert.Equal(2026, year);
        Assert.Equal(7, month);
    }

    [Fact]
    public void Unpinning_returns_to_the_calendar()
    {
        var settings = SiteSettings.CreateDefault();
        settings.ShowMonth(2026, 3);

        settings.FollowCurrentMonth();

        Assert.Null(settings.EventsYear);
        Assert.Null(settings.EventsMonth);
        Assert.Equal(8, settings.ResolveEventsMonth(LateJulyUtc).Month);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void A_month_outside_the_year_is_refused(int month)
    {
        var settings = SiteSettings.CreateDefault();

        Assert.Throws<DomainException>(() => settings.ShowMonth(2026, month));
    }

    [Fact]
    public void A_year_far_out_is_refused()
    {
        var settings = SiteSettings.CreateDefault();

        // Pinning a decade ahead is a typo, not a plan.
        Assert.Throws<DomainException>(() => settings.ShowMonth(DateTime.UtcNow.Year + 10, 6));
    }
}
