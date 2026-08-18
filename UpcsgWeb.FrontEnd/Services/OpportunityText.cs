using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

// Wording shared by the card, the planner and the detail page. Kept in one place because
// "closes tomorrow" appearing three different ways on the same page reads as a bug.
public static class OpportunityText
{
    public static string KindLabel(OpportunityKindDto kind) => kind switch
    {
        OpportunityKindDto.CallForPapers => "Call for papers",
        _ => kind.ToString(),
    };

    // Every method below checks IsDateTentative before touching DaysLeft, which is null for a
    // tentative entry. Falling through to `?? 0` would read it as "closes today".
    public static string Deadline(OpportunityDto item)
    {
        if (item.IsDateTentative)
        {
            var pencilled = item.ClosesAt ?? item.HappensAt;

            return pencilled is null
                ? "Date to be announced"
                : $"Around {pencilled.Value.ToLocalTime():MMMM yyyy}";
        }

        if (item.ClosesAt is null)
        {
            return item.HappensAt is null
                ? "No deadline given"
                : $"Happens {item.HappensAt.Value.ToLocalTime():MMM d}";
        }

        var days = item.DaysLeft ?? 0;

        return days switch
        {
            < 0 => "Closed",
            0 => "Closes today",
            1 => "Closes tomorrow",
            <= 14 => $"Closes in {days} days",
            _ => $"Closes {item.ClosesAt.Value.ToLocalTime():MMM d}",
        };
    }

    public static string Countdown(OpportunityDto item)
    {
        if (item.IsDateTentative)
        {
            return "Soon";
        }

        if (item.ClosesAt is null)
        {
            return "Open";
        }

        var days = item.DaysLeft ?? 0;

        return days switch
        {
            < 0 => "Closed",
            0 => "Today",
            _ => days.ToString(),
        };
    }

    public static string CountdownUnit(OpportunityDto item)
    {
        if (item.IsDateTentative)
        {
            var pencilled = item.ClosesAt ?? item.HappensAt;

            return pencilled is null
                ? "Date to be announced"
                : $"Pencilled in for {pencilled.Value.ToLocalTime():MMMM yyyy}";
        }

        if (item.ClosesAt is null)
        {
            return "No deadline given";
        }

        var days = item.DaysLeft ?? 0;

        return days switch
        {
            < 0 => "This one has passed",
            0 => "Last day to apply",
            1 => "day left",
            _ => "days left",
        };
    }

    public static string Urgency(OpportunityDto item)
    {
        if (item.IsDateTentative)
        {
            return "tentative";
        }

        if (item.ClosesAt is null)
        {
            return "open";
        }

        return (item.DaysLeft ?? 0) switch
        {
            < 0 => "closed",
            <= 3 => "urgent",
            <= 10 => "soon",
            _ => "open",
        };
    }

    public static string Summarise(string description)
    {
        var first = description
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return first.Length <= 180 ? first : first[..180].TrimEnd() + "…";
    }

    public static IEnumerable<string> Paragraphs(string? description) =>
        (description ?? string.Empty)
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Replace('\n', ' '));
}
