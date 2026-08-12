namespace UpcsgWeb.Shared.Contracts;

public class ContentSnapshot
{
    public int Version { get; set; } = 1;

    public DateTime GeneratedAt { get; set; }

    public List<MemberDto> Members { get; set; } = [];

    public List<EventDto> Events { get; set; } = [];

    public List<AchievementDto> Achievements { get; set; } = [];

    public List<MerchItemDto> Merch { get; set; } = [];

    public List<OpportunityDto> Opportunities { get; set; } = [];

    public SiteSettingsDto Settings { get; set; } = new();
}
