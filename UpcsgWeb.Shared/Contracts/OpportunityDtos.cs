namespace UpcsgWeb.Shared.Contracts;

public enum OpportunityKindDto
{
    Hackathon,
    Competition,
    Quizbowl,
    Conference,
    Scholarship,
    Internship,
    CallForPapers,
    Other,
}

public class OpportunityDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OpportunityKindDto Kind { get; set; }
    public string? Organiser { get; set; }
    public string? Location { get; set; }

    public DateTimeOffset? OpensAt { get; set; }
    public DateTimeOffset? ClosesAt { get; set; }
    public DateTimeOffset? HappensAt { get; set; }

    public string? Url { get; set; }
    public string? PosterUrl { get; set; }

    public bool IsFeatured { get; set; }

    /// <summary>
    /// The dates here are a best guess rather than an announcement.
    /// </summary>
    public bool IsDateTentative { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // A tentative entry never reads as closed: its deadline is a placeholder, so letting it
    // elapse would archive something that has not actually happened yet.
    public bool IsClosed =>
        !IsDateTentative && ClosesAt is not null && ClosesAt < DateTimeOffset.UtcNow;

    public bool HasDeadline => ClosesAt is not null;

    /// <summary>Flagged tentative and carrying no date at all, so there is nothing to show.</summary>
    public bool IsDateUnannounced => IsDateTentative && ClosesAt is null && HappensAt is null;

    /// <summary>
    /// A countdown only means something against a confirmed deadline. Tentative entries
    /// advertise themselves as coming soon instead.
    /// </summary>
    public bool ShowsCountdown => !IsDateTentative && ClosesAt is not null;

    // Whole days between calendar dates, not elapsed hours: an entry closing at the end of today
    // reads "closes today", not "closes tomorrow" because 14 hours are left on the clock. The
    // dates are local ones - this is recomputed in the browser, so the reader's own midnight is
    // the one that counts, not the API server's.
    // Null while the date is tentative, so callers that rank or headline by urgency (the home
    // page deadline kicker, for one) skip it rather than counting down to a placeholder.
    public int? DaysLeft => ClosesAt is null || IsDateTentative
        ? null
        : (ClosesAt.Value.ToLocalTime().Date - DateTimeOffset.Now.LocalDateTime.Date).Days;
}
