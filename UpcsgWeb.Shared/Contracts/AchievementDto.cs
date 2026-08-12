namespace UpcsgWeb.Shared.Contracts;

public class AchievementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
