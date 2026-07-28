namespace UpcsgWeb.Shared.Contracts;

public class MerchItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Variants { get; set; } = [];
    public bool InStock { get; set; } = true;
}

