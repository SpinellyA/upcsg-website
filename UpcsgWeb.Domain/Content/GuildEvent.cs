using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public class GuildEvent : AggregateRoot
{
    private GuildEvent() { }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Null when the event is announced before a date exists for it. Anything reading this
    /// for placement on a calendar should go through <see cref="IsScheduled"/> first.
    /// </summary>
    public DateTime? StartDateTime { get; private set; }

    public DateTime? EndDateTime { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string? PosterUrl { get; private set; }

    /// <summary>
    /// The date is a best guess rather than an announcement. Always true when there is no
    /// start date at all, because nothing about the timing is settled in that case.
    /// </summary>
    public bool IsDateTentative { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Has a date firm enough to put on the calendar. Tentative events are deliberately
    /// excluded: pinning one to a day asserts a date nobody has confirmed.
    /// </summary>
    public bool IsScheduled => StartDateTime is not null && !IsDateTentative;

    /// <summary>Announced, but not yet pinned to a confirmed date.</summary>
    public bool IsComingSoon => !IsScheduled;

    public static GuildEvent Create(
        string title,
        string description,
        DateTime? startsAt,
        DateTime? endsAt,
        string location,
        string? posterUrl = null,
        bool isDateTentative = false)
    {
        var guildEvent = new GuildEvent { Id = Guid.CreateVersion7() };
        guildEvent.Update(title, description, startsAt, endsAt, location, posterUrl, isDateTentative);
        return guildEvent;
    }

    public void Update(
        string title,
        string description,
        DateTime? startsAt,
        DateTime? endsAt,
        string location,
        string? posterUrl,
        bool isDateTentative = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An event needs a title.");
        }

        if (startsAt is null && endsAt is not null)
        {
            throw new DomainException(
                "An event cannot have an end time without a start time.");
        }

        if (startsAt is not null && endsAt is not null && endsAt <= startsAt)
        {
            throw new DomainException("An event cannot end before it starts.");
        }

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;

        StartDateTime = startsAt?.ToUniversalTime();
        EndDateTime = endsAt?.ToUniversalTime();

        // No date means nothing about the timing is settled, so the tentative flag is not the
        // officer's to turn off in that case. Without this an event could claim a firm date
        // while having none, and IsScheduled would let it onto the calendar with nowhere to go.
        IsDateTentative = startsAt is null || isDateTentative;

        Location = location?.Trim() ?? string.Empty;
        PosterUrl = posterUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
