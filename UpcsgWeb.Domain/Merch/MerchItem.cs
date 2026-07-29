using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Merch;

/// <summary>A purchasable item. Root of its own aggregate; orders reference it by id.</summary>
public class MerchItem : AggregateRoot
{
    private readonly List<MerchVariant> _variants = [];
    private readonly List<string> _photoUrls = [];

    private MerchItem() { } // EF

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Base price, used when the item has no variants. Once variants exist they each carry
    /// their own price and this is only a fallback — see <see cref="PriceFrom"/>.
    /// </summary>
    public Money Price { get; private set; } = Money.Zero();

    public bool InStock { get; private set; } = true;

    /// <summary>Ordering is meaningful: the first photo is the one listings show.</summary>
    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    public IReadOnlyList<MerchVariant> Variants =>
        _variants.OrderBy(v => v.DisplayOrder).ToList().AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// The number a listing should show. With variants priced separately, the honest
    /// headline is the cheapest way to buy the thing — anything else advertises a price
    /// the guilder may not be able to get.
    /// </summary>
    public Money PriceFrom => _variants.Count == 0
        ? Price
        : _variants.MinBy(v => v.Price.Amount)!.Price;

    /// <summary>True when variants disagree on price, so the UI can say "from".</summary>
    public bool HasPriceRange =>
        _variants.Count > 1 && _variants.Select(v => v.Price.Amount).Distinct().Count() > 1;

    public static MerchItem Create(string name, string description, Money price)
    {
        var item = new MerchItem();
        item.UpdateDetails(name, description, price);
        return item;
    }

    public void UpdateDetails(string name, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Merch needs a name.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;

        // Existing order lines keep their snapshotted price — repricing here is safe.
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplacePhotos(IEnumerable<string> photoUrls)
    {
        _photoUrls.Clear();

        foreach (var url in photoUrls ?? [])
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                _photoUrls.Add(url.Trim());
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public MerchVariant AddVariant(string name, string description, Money price, IEnumerable<string>? photoUrls = null)
    {
        if (_variants.Any(v => v.NameMatches(name)))
        {
            throw new DomainException($"{Name} already has a variant called '{name.Trim()}'.");
        }

        var variant = MerchVariant.Create(name, description, price, _variants.Count);
        variant.ReplacePhotos(photoUrls ?? []);
        _variants.Add(variant);
        UpdatedAt = DateTime.UtcNow;

        return variant;
    }

    public void UpdateVariant(int variantId, string name, string description, Money price, IEnumerable<string>? photoUrls)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        // A rename must not collide with a sibling, or cart lines keyed by name become
        // ambiguous about which variant they meant.
        if (_variants.Any(v => v.Id != variantId && v.NameMatches(name)))
        {
            throw new DomainException($"{Name} already has a variant called '{name.Trim()}'.");
        }

        variant.Update(name, description, price);
        variant.ReplacePhotos(photoUrls ?? []);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVariant(int variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        _variants.Remove(variant);
        Resequence();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReorderVariants(IReadOnlyList<int> variantIdsInOrder)
    {
        for (var i = 0; i < variantIdsInOrder.Count; i++)
        {
            _variants.FirstOrDefault(v => v.Id == variantIdsInOrder[i])?.SetDisplayOrder(i);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasVariant(string variant) => _variants.Any(v => v.NameMatches(variant));

    public MerchVariant? FindVariant(string? variant) =>
        variant is null ? null : _variants.FirstOrDefault(v => v.NameMatches(variant));

    /// <summary>
    /// What a line for this item and variant should cost. Falls back to the item price so
    /// an item with no variants still prices correctly.
    /// </summary>
    public Money PriceFor(string? variant) => FindVariant(variant)?.Price ?? Price;

    /// <summary>
    /// Photos to show for a selection: the variant's own, or the item's when the variant
    /// has none of its own. A variant without photos should look like the item, not blank.
    /// </summary>
    public IReadOnlyList<string> PhotosFor(string? variant)
    {
        var found = FindVariant(variant);
        return found is { PhotoUrls.Count: > 0 } ? found.PhotoUrls : PhotoUrls;
    }

    public void SetStock(bool inStock)
    {
        InStock = inStock;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Resequence()
    {
        var ordered = _variants.OrderBy(v => v.DisplayOrder).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetDisplayOrder(i);
        }
    }
}
