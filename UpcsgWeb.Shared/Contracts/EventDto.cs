namespace UpcsgWeb.Shared.Contracts;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Null when the event was announced before it had a date.</summary>
    public DateTime? StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }

    /// <summary>The date is a best guess. Always true when there is no start date.</summary>
    public bool IsDateTentative { get; set; }

    /// <summary>Firm enough to place on the calendar.</summary>
    public bool IsScheduled => StartDateTime is not null && !IsDateTentative;

    /// <summary>Announced, but not pinned to a confirmed date.</summary>
    public bool IsComingSoon => !IsScheduled;
}
