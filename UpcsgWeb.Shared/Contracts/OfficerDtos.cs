namespace UpcsgWeb.Shared.Contracts;

/// <summary>An address that gets officer rights when it signs in.</summary>
public class OfficerEmailDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Who this is, so a handover isn't a list of anonymous addresses.</summary>
    public string? Note { get; set; }

    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Whether an account with this address exists yet. False right after a handover is
    /// normal; false weeks later usually means a typo.
    /// </summary>
    public bool HasSignedIn { get; set; }
}

public class AddOfficerRequest
{
    public string Email { get; set; } = string.Empty;

    public string? Note { get; set; }
}
