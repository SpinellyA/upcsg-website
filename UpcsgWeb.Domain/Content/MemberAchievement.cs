using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

// A line on an officer's record: what they did, and when if they remember. Kept as a
// value object rather than a table because it is only ever read as part of the member,
// and never queried on its own.
public class MemberAchievement
{
    private MemberAchievement() { }

    public string Title { get; private set; } = string.Empty;

    public int? Year { get; private set; }

    public static MemberAchievement Of(string title, int? year)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An achievement needs a description.");
        }

        if (year is { } y && (y < 1900 || y > DateTime.UtcNow.Year + 1))
        {
            throw new DomainException("That achievement year does not look right.");
        }

        return new MemberAchievement
        {
            Title = title.Trim(),
            Year = year,
        };
    }
}
