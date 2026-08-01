using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Where an event sits relative to now, in the terms someone would actually say.
///
/// This lived inside the detail page, so the events list had no way to say an event was
/// finished without reimplementing it. Two copies of "has this ended" is exactly the
/// drift that puts a "Starts in 3 days" badge on something that happened last week.
///
/// Every method reads instants as Cebu wall-clock. The API stores UTC, and comparing a
/// UTC instant against DateTime.Now would call a 6 PM event finished eight hours early.
/// </summary>
public static class EventTiming
{
    public static DateTime Starts(EventDto e) => e.StartDateTime.ToLocalTime();

    public static DateTime? Ends(EventDto e) => e.EndDateTime?.ToLocalTime();

    /// <summary>An event with no end time is treated as over once it has started.</summary>
    public static DateTime Finishes(EventDto e) => Ends(e) ?? Starts(e);

    public static bool HasEnded(EventDto e) => Finishes(e) < DateTime.Now;

    public static bool IsUnderway(EventDto e) => Starts(e) <= DateTime.Now && !HasEnded(e);

    public static string StatusLabel(EventDto e) =>
        IsUnderway(e) ? "Happening now" : HasEnded(e) ? "Finished" : "Upcoming";

    /// <summary>
    /// Reuses the order status pill colours: gold for "needs attention now", muted for
    /// done, lavender for pending.
    /// </summary>
    public static string StatusSlug(EventDto e) =>
        IsUnderway(e) ? "pending" : HasEnded(e) ? "received" : "confirmed";

    public static string TimeRange(EventDto e) =>
        Ends(e) is { } ends
            ? $"{Starts(e):h:mm tt} – {ends:h:mm tt}"
            : Starts(e).ToString("h:mm tt");

    public static string DurationLabel(EventDto e)
    {
        if (Ends(e) is not { } ends)
        {
            return "Not announced";
        }

        var span = ends - Starts(e);
        var hours = (int)span.TotalHours;

        if (hours == 0)
        {
            return $"{span.Minutes} min";
        }

        return span.Minutes == 0 ? $"{hours} hr" : $"{hours} hr {span.Minutes} min";
    }

    /// <summary>How far off it is, phrased the way a person would say it.</summary>
    public static string Countdown(EventDto e)
    {
        if (IsUnderway(e))
        {
            return "Happening now";
        }

        if (HasEnded(e))
        {
            var since = DateTime.Now - Finishes(e);
            return since.TotalDays >= 1 ? $"Finished {Describe(since)} ago" : "Finished earlier today";
        }

        var until = Starts(e) - DateTime.Now;

        return until.TotalMinutes < 60
            ? $"Starts in {Math.Max(1, (int)until.TotalMinutes)} min"
            : $"Starts in {Describe(until)}";
    }

    private static string Describe(TimeSpan span)
    {
        var days = (int)span.TotalDays;

        if (days >= 14)
        {
            return $"{days / 7} weeks";
        }

        if (days >= 1)
        {
            return days == 1 ? "a day" : $"{days} days";
        }

        var hours = (int)span.TotalHours;
        return hours <= 1 ? "an hour" : $"{hours} hours";
    }

    /// <summary>
    /// Words that mean "there is nowhere to walk to". Checked against the location the
    /// officers type, since the record has no separate flag for it.
    /// </summary>
    private static readonly string[] OnlineWords =
        ["online", "google meet", "meet.google", "zoom", "discord", "virtual", "webinar", "stream"];

    public static bool IsOnline(EventDto e) =>
        OnlineWords.Any(w => e.Location.Contains(w, StringComparison.OrdinalIgnoreCase));
}
