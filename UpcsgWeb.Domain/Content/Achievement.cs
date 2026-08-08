using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public class Achievement : AggregateRoot
{
    private Achievement() { }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Category { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static Achievement Create(string title, string description, int year, string? category)
    {
        var achievement = new Achievement { Id = Guid.CreateVersion7() };
        achievement.Update(title, description, year, category, null);
        return achievement;
    }

    public void Update(string title, string description, int year, string? category, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("An achievement needs a title.");
        }

        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
        {
            throw new DomainException($"Year {year} is out of range.");
        }

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Year = year;
        Category = category?.Trim();
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
