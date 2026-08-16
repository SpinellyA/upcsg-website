namespace UpcsgWeb.Shared.Contracts;

public enum MemberCategory
{
    Faculty,
    ExeCom
}

public enum MemberLinkKindDto
{
    Email,
    Facebook,
    Instagram,
    LinkedIn,
    GitHub,
    Website,
}

public class MemberAchievementDto
{
    public string Title { get; set; } = string.Empty;

    public int? Year { get; set; }
}

public class MemberLinkDto
{
    public MemberLinkKindDto Kind { get; set; }

    public string Value { get; set; } = string.Empty;

    // The profile needs an href, and an email is not a URL. The rule lives here rather
    // than in every view that renders a contact.
    public string Href => Kind == MemberLinkKindDto.Email ? $"mailto:{Value}" : Value;

    public bool IsEmail => Kind == MemberLinkKindDto.Email;

    // An email reads as itself; a link reads better as host and path than as the whole
    // "https://www.facebook.com/..." string.
    public string Display
    {
        get
        {
            if (IsEmail)
            {
                return Value;
            }

            if (!Uri.TryCreate(Value, UriKind.Absolute, out var parsed))
            {
                return Value;
            }

            var host = parsed.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? parsed.Host[4..]
                : parsed.Host;

            var path = parsed.AbsolutePath.Trim('/');

            return string.IsNullOrEmpty(path) ? host : $"{host}/{path}";
        }
    }
}

public class MemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public MemberCategory Category { get; set; }
    public string? Committee { get; set; }
    public string? PhotoUrl { get; set; }

    public string? Quote { get; set; }

    public string? Bio { get; set; }

    public string? Profile { get; set; }

    public List<MemberAchievementDto> Achievements { get; set; } = [];

    public List<MemberLinkDto> Links { get; set; } = [];

    public int DisplayOrder { get; set; }

    // Whether "Know more" has anything to show beyond what the page already says.
    public bool HasProfile =>
        !string.IsNullOrWhiteSpace(Profile) || Achievements.Count > 0 || Links.Count > 0;
}
