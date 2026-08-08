namespace UpcsgWeb.Shared.Contracts;

public class MerchVariantDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public List<string> PhotoUrls { get; set; } = [];
}

public class MerchItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public List<string> PhotoUrls { get; set; } = [];

    public List<MerchVariantDto> Variants { get; set; } = [];

    public bool InStock { get; set; } = true;

    public decimal SalePercentage { get; set; }

    public bool IsOnSale { get; set; }

    public bool IsPreorder { get; set; }

    public DateTime? PreorderClosesAt { get; set; }

    public decimal PriceFrom { get; set; }

    public decimal ListPriceFrom { get; set; }

    public bool HasPriceRange { get; set; }

    public bool HasActiveSale { get; set; }

    public bool IsPreorderClosed { get; set; }

    public string? ImageUrl => PhotoUrls.FirstOrDefault();

    public decimal PriceOf(string? variant) =>
        HasActiveSale
            ? decimal.Round(ListPriceOf(variant) * (1m - SalePercentage / 100m), 2, MidpointRounding.ToEven)
            : ListPriceOf(variant);

    public decimal ListPriceOf(string? variant) =>
        FindVariant(variant)?.Price ?? Price;

    public int StockOf(string? variant) =>
        IsPreorder ? int.MaxValue : FindVariant(variant)?.Stock ?? Stock;

    public bool CanBuy(string? variant, int quantity) =>
        InStock && !IsPreorderClosed && StockOf(variant) >= quantity;

    private MerchVariantDto? FindVariant(string? variant) =>
        Variants.FirstOrDefault(v => string.Equals(v.Name, variant, StringComparison.OrdinalIgnoreCase));

    public List<string> PhotosOf(string? variant)
    {
        var found = Variants.FirstOrDefault(v => string.Equals(v.Name, variant, StringComparison.OrdinalIgnoreCase));
        return found is { PhotoUrls.Count: > 0 } ? found.PhotoUrls : PhotoUrls;
    }
}
