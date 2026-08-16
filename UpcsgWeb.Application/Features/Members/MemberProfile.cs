using UpcsgWeb.Domain.Content;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Members;

// Turning the wire shape into value objects, shared by create and update so the two
// paths cannot drift. Blank rows are dropped rather than rejected: the CMS form keeps
// an empty row at the bottom for the next entry, and an officer who saves without
// filling it in has not made a mistake.
internal static class MemberProfile
{
    internal static IEnumerable<MemberAchievement> AchievementsFrom(MemberDto dto) =>
        dto.Achievements
            .Where(a => !string.IsNullOrWhiteSpace(a.Title))
            .Select(a => MemberAchievement.Of(a.Title, a.Year));

    internal static IEnumerable<MemberLink> LinksFrom(MemberDto dto) =>
        dto.Links
            .Where(l => !string.IsNullOrWhiteSpace(l.Value))
            .Select(l => MemberLink.Of(
                Enum.Parse<MemberLinkKind>(l.Kind.ToString()),
                l.Value));
}
