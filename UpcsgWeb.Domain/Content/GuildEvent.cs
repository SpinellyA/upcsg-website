using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

/// <summary>
/// A guild event. Named GuildEvent rather than Event to stay clear of the `event`
/// keyword and of domain-event terminology.
/// </summary>
public class GuildEvent : AggregateRoot
{
    private GuildEvent() { } // EF

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public string? PosterUrl { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static GuildEvent Create(
        string title,
        string description,
        DateTime startsAt,
        DateTime? endsAt,
        string location,
        string? posterUrl = null)
    {
        var guildEvent = new GuildEvent { Id = Guid.CreateVersion7() };
        guildEvent.Update(title, description, startsAt, endsAt, location, posterUrl);
        return guildEvent;
    }

    public void Update(
        string title,
        string description,
        DateTime startsAt,
        DateTime? endsAt,
        string location,
        string? posterUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An event needs a title.");
        }

        if (endsAt is not null && endsAt <= startsAt)
        {
            throw new DomainException("An event cannot end before it starts.");
        }

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;

        // Npgsql rejects non-UTC values on timestamptz, so normalise at the boundary.
        StartDateTime = startsAt.ToUniversalTime();
        EndDateTime = endsAt?.ToUniversalTime();

        Location = location?.Trim() ?? string.Empty;
        PosterUrl = posterUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
