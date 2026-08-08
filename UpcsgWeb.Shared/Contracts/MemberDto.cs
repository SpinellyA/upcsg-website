namespace UpcsgWeb.Shared.Contracts;

public enum MemberCategory
{
    Faculty,
    ExeCom
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

    public int DisplayOrder { get; set; }
}
