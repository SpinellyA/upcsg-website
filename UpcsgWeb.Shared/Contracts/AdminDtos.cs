namespace UpcsgWeb.Shared.Contracts;

/// <summary>Which month the public events calendar renders.</summary>
public class SiteSettingsDto
{
    /// <summary>Null when the site simply follows the real calendar.</summary>
    public int? EventsYear { get; set; }

    public int? EventsMonth { get; set; }

    /// <summary>Resolved month the events page should request.</summary>
    public int ResolvedYear { get; set; }

    public int ResolvedMonth { get; set; }

    public bool FollowsCurrentMonth => EventsYear is null || EventsMonth is null;
}

public class UpdateSiteSettingsRequest
{
    /// <summary>False pins the calendar to the supplied year/month.</summary>
    public bool FollowCurrentMonth { get; set; } = true;

    public int? EventsYear { get; set; }
    public int? EventsMonth { get; set; }
}
