using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public static class EventTiming
{
    // Null for an event announced before it had a date. Everything below treats that case as
    // "not started, not ended", so an undated event never reads as finished and never claims
    // to be happening now.
    public static DateTime? Starts(EventDto e) => e.StartDateTime?.ToLocalTime();

    public static DateTime? Ends(EventDto e) => e.EndDateTime?.ToLocalTime();

    public static DateTime? Finishes(EventDto e) => Ends(e) ?? Starts(e);

    public static bool HasEnded(EventDto e) => Finishes(e) is { } f && f < DateTime.Now;

    public static bool IsUnderway(EventDto e) =>
        Starts(e) is { } s && s <= DateTime.Now && !HasEnded(e);

    /// <summary>The rough date an unscheduled event is pencilled in for, if there is one.</summary>
    public static string? PencilledIn(EventDto e) =>
        Starts(e) is { } s ? s.ToString("MMMM yyyy") : null;

    public static string StatusLabel(EventDto e) =>
        e.IsComingSoon ? "Coming soon"
        : IsUnderway(e) ? "Happening now"
        : HasEnded(e) ? "Finished"
        : "Upcoming";

    public static string StatusSlug(EventDto e) =>
        e.IsComingSoon ? "pending"
        : IsUnderway(e) ? "pending"
        : HasEnded(e) ? "received"
        : "confirmed";

    public static string TimeRange(EventDto e)
    {
        if (e.IsComingSoon)
        {
            return PencilledIn(e) is { } month ? $"Around {month}" : "Date to be announced";
        }

        return Ends(e) is { } ends
            ? $"{Starts(e):h:mm tt} – {ends:h:mm tt}"
            : Starts(e)?.ToString("h:mm tt") ?? "Date to be announced";
    }

    public static string DurationLabel(EventDto e)
    {
        if (Starts(e) is not { } starts || Ends(e) is not { } ends)
        {
            return "Not announced";
        }

        var span = ends - starts;
        var hours = (int)span.TotalHours;

        if (hours == 0)
        {
            return $"{span.Minutes} min";
        }

        return span.Minutes == 0 ? $"{hours} hr" : $"{hours} hr {span.Minutes} min";
    }

    public static string Countdown(EventDto e)
    {
        // Checked before the underway/ended pair, both of which are false for an undated
        // event and would otherwise fall through to a countdown against nothing.
        if (e.IsComingSoon)
        {
            return PencilledIn(e) is { } month ? $"Pencilled in for {month}" : "Date to be announced";
        }

        if (IsUnderway(e))
        {
            return "Happening now";
        }

        if (HasEnded(e))
        {
            var since = DateTime.Now - Finishes(e)!.Value;
            return since.TotalDays >= 1 ? $"Finished {Describe(since)} ago" : "Finished earlier today";
        }

        var until = Starts(e)!.Value - DateTime.Now;

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

    private static readonly string[] OnlineWords =
        ["online", "google meet", "meet.google", "zoom", "discord", "virtual", "webinar", "stream"];

    public static bool IsOnline(EventDto e) =>
        OnlineWords.Any(w => e.Location.Contains(w, StringComparison.OrdinalIgnoreCase));
}
