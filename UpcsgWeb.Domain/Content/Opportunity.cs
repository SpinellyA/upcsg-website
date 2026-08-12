using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public enum OpportunityKind
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

public class Opportunity : AggregateRoot
{
    private Opportunity() { }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public OpportunityKind Kind { get; private set; }
    public string? Organiser { get; private set; }
    public string? Location { get; private set; }

    public DateTime? OpensAt { get; private set; }
    public DateTime? ClosesAt { get; private set; }
    public DateTime? HappensAt { get; private set; }

    public string? Url { get; private set; }
    public string? PosterUrl { get; private set; }

    public bool IsFeatured { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsClosed => ClosesAt is not null && ClosesAt < DateTime.UtcNow;

    public static Opportunity Create(
        string title,
        string description,
        OpportunityKind kind,
        string? organiser,
        string? location,
        DateTime? opensAt,
        DateTime? closesAt,
        DateTime? happensAt,
        string? url,
        string? posterUrl = null)
    {
        var opportunity = new Opportunity { Id = Guid.CreateVersion7() };

        opportunity.Update(
            title, description, kind, organiser, location,
            opensAt, closesAt, happensAt, url, posterUrl);

        return opportunity;
    }

    public void Update(
        string title,
        string description,
        OpportunityKind kind,
        string? organiser,
        string? location,
        DateTime? opensAt,
        DateTime? closesAt,
        DateTime? happensAt,
        string? url,
        string? posterUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An opportunity needs a title.");
        }

        if (opensAt is not null && closesAt is not null && closesAt <= opensAt)
        {
            throw new DomainException("An opportunity cannot close before it opens.");
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            var wellFormed = Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

            if (!wellFormed)
            {
                throw new DomainException("The link must be a full http or https URL.");
            }
        }

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Kind = kind;
        Organiser = Blank(organiser);
        Location = Blank(location);

        OpensAt = opensAt?.ToUniversalTime();
        ClosesAt = closesAt?.ToUniversalTime();
        HappensAt = happensAt?.ToUniversalTime();

        Url = Blank(url);
        PosterUrl = Blank(posterUrl);

        UpdatedAt = DateTime.UtcNow;
    }

    public void Feature(bool featured)
    {
        IsFeatured = featured;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
