namespace UpcsgWeb.Shared.Contracts;

public class OfficerEmailDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime AddedAt { get; set; }

    public bool HasSignedIn { get; set; }
}

public class AddOfficerRequest
{
    public string Email { get; set; } = string.Empty;

    public string? Note { get; set; }
}
