using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Domain.Tests;

public class GuildEventSchedulingTests
{
    private static GuildEvent Dated(DateTime startsAt, bool tentative = false) =>
        GuildEvent.Create("General Assembly", "", startsAt, null, "AVR", null, tentative);

    private static GuildEvent Undated() =>
        GuildEvent.Create("General Assembly", "", null, null, "AVR", null);

    [Fact]
    public void AnEventWithAConfirmedDateIsScheduled()
    {
        var guildEvent = Dated(new DateTime(2026, 3, 14, 18, 0, 0, DateTimeKind.Utc));

        Assert.True(guildEvent.IsScheduled);
        Assert.False(guildEvent.IsComingSoon);
        Assert.False(guildEvent.IsDateTentative);
    }

    [Fact]
    public void AnEventWithNoDateIsComingSoon()
    {
        var guildEvent = Undated();

        Assert.Null(guildEvent.StartDateTime);
        Assert.False(guildEvent.IsScheduled);
        Assert.True(guildEvent.IsComingSoon);
    }

    // Without this, an officer could clear the date and leave the flag off, and the event
    // would claim a firm date while having none - putting it on a calendar with no day.
    [Fact]
    public void DroppingTheDateForcesTheEventTentative()
    {
        var guildEvent = Dated(new DateTime(2026, 3, 14, 18, 0, 0, DateTimeKind.Utc));
        Assert.False(guildEvent.IsDateTentative);

        guildEvent.Update("General Assembly", "", null, null, "AVR", null, isDateTentative: false);

        Assert.True(guildEvent.IsDateTentative);
        Assert.True(guildEvent.IsComingSoon);
    }

    // A rough date is still not a date. It orders the coming-soon list, but the event stays
    // off the calendar, because pinning it to a day asserts something nobody confirmed.
    [Fact]
    public void ATentativeDateDoesNotMakeAnEventScheduled()
    {
        var guildEvent = Dated(new DateTime(2026, 3, 1, 18, 0, 0, DateTimeKind.Utc), tentative: true);

        Assert.NotNull(guildEvent.StartDateTime);
        Assert.False(guildEvent.IsScheduled);
        Assert.True(guildEvent.IsComingSoon);
    }

    [Fact]
    public void ConfirmingADateMakesATentativeEventScheduled()
    {
        var guildEvent = Undated();

        guildEvent.Update(
            "General Assembly", "",
            new DateTime(2026, 3, 14, 18, 0, 0, DateTimeKind.Utc), null,
            "AVR", null, isDateTentative: false);

        Assert.True(guildEvent.IsScheduled);
        Assert.False(guildEvent.IsDateTentative);
    }

    [Fact]
    public void AnEndTimeWithoutAStartTimeIsRefused()
    {
        var ex = Assert.Throws<DomainException>(() => GuildEvent.Create(
            "General Assembly", "",
            null,
            new DateTime(2026, 3, 14, 20, 0, 0, DateTimeKind.Utc),
            "AVR", null));

        Assert.Contains("end time", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnEventStillCannotEndBeforeItStarts()
    {
        Assert.Throws<DomainException>(() => GuildEvent.Create(
            "General Assembly", "",
            new DateTime(2026, 3, 14, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 14, 18, 0, 0, DateTimeKind.Utc),
            "AVR", null));
    }

    [Fact]
    public void AnEventStillNeedsATitle()
    {
        Assert.Throws<DomainException>(() => GuildEvent.Create("  ", "", null, null, "AVR", null));
    }
}
