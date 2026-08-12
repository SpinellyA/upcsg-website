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

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsClosed => ClosesAt is not null && ClosesAt < DateTimeOffset.UtcNow;

    public bool HasDeadline => ClosesAt is not null;

    // Whole days between calendar dates, not elapsed hours: an entry closing at the end of today
    // reads "closes today", not "closes tomorrow" because 14 hours are left on the clock. The
    // dates are local ones - this is recomputed in the browser, so the reader's own midnight is
    // the one that counts, not the API server's.
    public int? DaysLeft => ClosesAt is null
        ? null
        : (ClosesAt.Value.ToLocalTime().Date - DateTimeOffset.Now.LocalDateTime.Date).Days;
}
