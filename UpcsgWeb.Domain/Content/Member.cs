using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public class Member : AggregateRoot
{
    private Member() { }

    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public MemberCategory Category { get; private set; }
    public string? Committee { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Quote { get; private set; }
    public string? Bio { get; private set; }
    public int DisplayOrder { get; private set; }

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

    public void SetProfile(string? photoUrl, string? quote, string? bio)
    {
        PhotoUrl = photoUrl;
        Quote = quote?.Trim();
        Bio = bio?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
