namespace UpcsgWeb.Shared.Contracts;

public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
}

