namespace UpcsgWeb.Shared.Contracts;

public class MerchVariantDto
{
    /// <summary>Zero for a variant the CMS has added but not yet saved.</summary>
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Ordered. Empty means "show the item's photos instead".</summary>
    public List<string> PhotoUrls { get; set; } = [];
}

public class MerchItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Base price. Only what the guilder pays when the item has no variants.</summary>
    public decimal Price { get; set; }

    /// <summary>Ordered; the first is the listing thumbnail.</summary>
    public List<string> PhotoUrls { get; set; } = [];

    public List<MerchVariantDto> Variants { get; set; } = [];

    public bool InStock { get; set; } = true;

    // --- Server-computed, ignored on the way in -------------------------------------

    /// <summary>
    /// Cheapest way to buy the item. Listings show this rather than the base price, which
    /// with variants priced separately would be a number nobody can actually pay.
    /// </summary>
    public decimal PriceFrom { get; set; }

    /// <summary>True when variants disagree on price, so the UI can prefix "from".</summary>
    public bool HasPriceRange { get; set; }

    /// <summary>First photo, or null. Convenience for listings and cart rows.</summary>
    public string? ImageUrl => PhotoUrls.FirstOrDefault();

    /// <summary>Price for a named variant, falling back to the base price.</summary>
    public decimal PriceOf(string? variant) =>
        Variants.FirstOrDefault(v => string.Equals(v.Name, variant, StringComparison.OrdinalIgnoreCase))?.Price
        ?? Price;

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
