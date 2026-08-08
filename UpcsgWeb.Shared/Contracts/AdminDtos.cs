namespace UpcsgWeb.Shared.Contracts;

public class SiteSettingsDto
{
    public int? EventsYear { get; set; }

    public int? EventsMonth { get; set; }

    public int ResolvedYear { get; set; }

    public int ResolvedMonth { get; set; }

    public bool FollowsCurrentMonth => EventsYear is null || EventsMonth is null;
}

public class UpdateSiteSettingsRequest
{
    public bool FollowCurrentMonth { get; set; } = true;

    public int? EventsYear { get; set; }
    public int? EventsMonth { get; set; }
}
