namespace UpcsgWeb.Shared.Contracts;

public enum MemberCategory
{
    Faculty,
    ExeCom
}

public class MemberDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public MemberCategory Category { get; set; }
    public string? Committee { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>A short personal quote the member chose to share.</summary>
    public string? Quote { get; set; }

    /// <summary>Formal description of what this member does in the guild.</summary>
    public string? Bio { get; set; }

    public int DisplayOrder { get; set; }
}

