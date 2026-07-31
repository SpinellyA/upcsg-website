namespace UpcsgWeb.Shared.Contracts;

public class MerchVariantDto
{
    /// <summary>Zero for a variant the CMS has added but not yet saved.</summary>
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>List price for this variant, before any sale on the item.</summary>
    public decimal Price { get; set; }

    /// <summary>Units on hand. Meaningless while the item is a preorder.</summary>
    public int Stock { get; set; }

    /// <summary>Ordered. Empty means "show the item's photos instead".</summary>
    public List<string> PhotoUrls { get; set; } = [];
}

public class MerchItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Base list price. Only what the guilder pays when the item has no variants.</summary>
    public decimal Price { get; set; }

    /// <summary>Stock for an item with no variants. Mirrors how Price falls back.</summary>
    public int Stock { get; set; }

    /// <summary>Ordered; the first is the listing thumbnail.</summary>
    public List<string> PhotoUrls { get; set; } = [];

    public List<MerchVariantDto> Variants { get; set; } = [];

    /// <summary>Officer-facing shutter, independent of how many units remain.</summary>
    public bool InStock { get; set; } = true;

    /// <summary>Percentage off, never the discounted amount. 0–100.</summary>
    public decimal SalePercentage { get; set; }

    public bool IsOnSale { get; set; }

    /// <summary>Produced to demand: stock is ignored and it can never sell out.</summary>
    public bool IsPreorder { get; set; }

    /// <summary>When the preorder window shuts. Null means open-ended.</summary>
    public DateTime? PreorderClosesAt { get; set; }

    // --- Server-computed, ignored on the way in -------------------------------------

    /// <summary>
    /// Cheapest way to buy the item. Listings show this rather than the base price, which
    /// with variants priced separately would be a number nobody can actually pay.
    /// </summary>
    public decimal PriceFrom { get; set; }

    /// <summary>The same figure before any sale, so the UI can strike it through.</summary>
    public decimal ListPriceFrom { get; set; }

    /// <summary>True when variants disagree on price, so the UI can prefix "from".</summary>
    public bool HasPriceRange { get; set; }

    /// <summary>A sale switched on and actually worth something.</summary>
    public bool HasActiveSale { get; set; }

    /// <summary>The preorder window has shut; nothing more can be ordered.</summary>
    public bool IsPreorderClosed { get; set; }

    /// <summary>First photo, or null. Convenience for listings and cart rows.</summary>
    public string? ImageUrl => PhotoUrls.FirstOrDefault();

    /// <summary>What a variant actually costs, sale applied.</summary>
    public decimal PriceOf(string? variant) =>
        HasActiveSale
            ? decimal.Round(ListPriceOf(variant) * (1m - SalePercentage / 100m), 2, MidpointRounding.ToEven)
            : ListPriceOf(variant);

    /// <summary>The same before the sale, for the struck-through figure.</summary>
    public decimal ListPriceOf(string? variant) =>
        FindVariant(variant)?.Price ?? Price;

    /// <summary>
    /// Units left for a selection. Preorders report int.MaxValue: they are produced to
    /// demand, so there is no count to report.
    /// </summary>
    public int StockOf(string? variant) =>
        IsPreorder ? int.MaxValue : FindVariant(variant)?.Stock ?? Stock;

    /// <summary>Whether this many can be bought right now.</summary>
    public bool CanBuy(string? variant, int quantity) =>
        InStock && !IsPreorderClosed && StockOf(variant) >= quantity;

    private MerchVariantDto? FindVariant(string? variant) =>
        Variants.FirstOrDefault(v => string.Equals(v.Name, variant, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Photos to show for a selection: the variant's own, or the item's when it has none.
    /// A variant without photos should look like the item, not like a broken gallery.
    /// </summary>
    public List<string> PhotosOf(string? variant)
    {
        var found = Variants.FirstOrDefault(v => string.Equals(v.Name, variant, StringComparison.OrdinalIgnoreCase));
        return found is { PhotoUrls.Count: > 0 } ? found.PhotoUrls : PhotoUrls;
    }
}
