using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public class Member : AggregateRoot
{
    private readonly List<MemberAchievement> _achievements = [];
    private readonly List<MemberLink> _links = [];

    private Member() { }

    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public MemberCategory Category { get; private set; }
    public string? Committee { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Quote { get; private set; }

    // Bio is the paragraph beside the portrait on the About page; Profile is the long
    // form behind "Know more". Two fields because they are read in two places at two
    // lengths - a paragraph that sits well on the page is thin in the dialog, and one
    // that fills the dialog swamps the page.
    public string? Bio { get; private set; }
    public string? Profile { get; private set; }

    public int DisplayOrder { get; private set; }

    public IReadOnlyList<MemberAchievement> Achievements => _achievements.AsReadOnly();

    public IReadOnlyList<MemberLink> Links => _links.AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static Member Create(
        string name,
        string role,
        MemberCategory category,
        string? committee,
        int displayOrder)
    {
        var member = new Member { Id = Guid.CreateVersion7(), Category = category };
        member.Update(name, role, committee, displayOrder);
        return member;
    }

    public void Update(string name, string role, string? committee, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A member needs a name.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new DomainException("A member needs a role.");
        }

        Name = name.Trim();
        Role = role.Trim();
        Committee = committee?.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetProfile(string? photoUrl, string? quote, string? bio, string? profile)
    {
        PhotoUrl = photoUrl;
        Quote = quote?.Trim();
        Bio = bio?.Trim();
        Profile = profile?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAchievements(IEnumerable<MemberAchievement> achievements)
    {
        _achievements.Clear();
        _achievements.AddRange(achievements);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLinks(IEnumerable<MemberLink> links)
    {
        var incoming = links.ToList();

        // One address per kind. Two "Email" rows on a profile is an officer having added
        // rather than replaced, and the stale one is the one people will use.
        var duplicate = incoming
            .GroupBy(l => l.Kind)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new DomainException($"There are two {duplicate.Key} entries. Keep one.");
        }

        _links.Clear();
        _links.AddRange(incoming);
        UpdatedAt = DateTime.UtcNow;
    }
}
