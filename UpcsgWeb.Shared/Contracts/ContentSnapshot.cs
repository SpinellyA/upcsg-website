namespace UpcsgWeb.Shared.Contracts;

/// <summary>
/// Every piece of public content in one document, so the site can render with no API.
///
/// Only what the public pages read. Orders, carts, users and the officer allowlist are
/// deliberately absent: this file gets committed to a public repository, and a snapshot
/// that quietly carried order history would put guilders' purchases on GitHub.
/// </summary>
public class ContentSnapshot
{
    /// <summary>Schema version, so an old file on disk can be recognised rather than misread.</summary>
    public int Version { get; set; } = 1;

    public DateTime GeneratedAt { get; set; }

    public List<MemberDto> Members { get; set; } = [];

    /// <summary>
    /// Every event, not just the published month. The site decides which to show from
    /// <see cref="Settings"/>, so a snapshot taken in July still works in August.
    /// </summary>
    public List<EventDto> Events { get; set; } = [];

    public List<AchievementDto> Achievements { get; set; } = [];

    public List<MerchItemDto> Merch { get; set; } = [];

    public SiteSettingsDto Settings { get; set; } = new();
}
