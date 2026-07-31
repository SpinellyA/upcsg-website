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

    /// <summary>
    /// Officer-controlled shutter. Independent of stock counts: an item can be hidden from
    /// sale while units remain, and running out does not need this flipped.
    /// </summary>
    public bool InStock { get; private set; } = true;

    /// <summary>
    /// Discount applied to whatever the chosen variant costs.
    ///
    /// The PERCENTAGE is stored, never the discounted amount. Storing the sale price would
    /// lose the original when the sale ends, and re-applying a sale to an already-reduced
    /// number is how shops accidentally discount twice.
    /// </summary>
    public decimal SalePercentage { get; private set; }

    public bool IsOnSale { get; private set; }

    /// <summary>
    /// Produced to demand rather than held in stock. While true, stock counts are ignored
    /// and the item can never be out of stock — which is the whole point of a preorder.
    /// </summary>
    public bool IsPreorder { get; private set; }

    /// <summary>When the preorder window shuts. Null means open-ended.</summary>
    public DateTime? PreorderClosesAt { get; private set; }

    /// <summary>Stock for an item with no variants. Mirrors how Price falls back.</summary>
    public int Stock { get; private set; }

    /// <summary>Ordering is meaningful: the first photo is the one listings show.</summary>
    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    public IReadOnlyList<MerchVariant> Variants =>
        _variants.OrderBy(v => v.DisplayOrder).ToList().AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// The number a listing should show, after any sale. With variants priced separately,
    /// the honest headline is the cheapest way to buy the thing — anything else advertises
    /// a price the guilder may not be able to get.
    /// </summary>
    public Money PriceFrom => ApplySale(ListPriceFrom);

    /// <summary>The same figure before any discount, so the UI can strike it through.</summary>
    public Money ListPriceFrom => _variants.Count == 0
        ? Price
        : _variants.MinBy(v => v.Price.Amount)!.Price;

    /// <summary>True when variants disagree on price, so the UI can say "from".</summary>
    public bool HasPriceRange =>
        _variants.Count > 1 && _variants.Select(v => v.Price.Amount).Distinct().Count() > 1;

    /// <summary>A sale switched on and actually worth something.</summary>
    public bool HasActiveSale => IsOnSale && SalePercentage > 0m;

    /// <summary>
    /// Whether the preorder window has shut. A closed preorder stops accepting orders the
    /// same way an out-of-stock item does.
    /// </summary>
    public bool IsPreorderClosed =>
        IsPreorder && PreorderClosesAt is { } closes && closes <= DateTime.UtcNow;

    private Money ApplySale(Money list) =>
        HasActiveSale ? Money.Of(list.Amount * (1m - SalePercentage / 100m), list.Currency) : list;

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

    /// <summary>
    /// Stock is set here rather than afterwards: a new variant has no id until it is
    /// saved, so SetVariantStock could not find it — and with two new variants pending it
    /// would match the wrong one.
    /// </summary>
    public MerchVariant AddVariant(
        string name, string description, Money price, IEnumerable<string>? photoUrls = null, int stock = 0)
    {
        if (_variants.Any(v => v.NameMatches(name)))
        {
            throw new DomainException($"{Name} already has a variant called '{name.Trim()}'.");
        }

        var variant = MerchVariant.Create(name, description, price, _variants.Count);
        variant.ReplacePhotos(photoUrls ?? []);
        variant.SetStock(stock);
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
    /// What a line for this item and variant actually costs, sale included. This is the
    /// figure orders snapshot — deliberately the discounted one, so callers cannot charge
    /// the list price by forgetting to apply the sale.
    /// </summary>
    public Money PriceFor(string? variant) => ApplySale(ListPriceFor(variant));

    /// <summary>
    /// The undiscounted price. Falls back to the item price so an item with no variants
    /// still prices correctly.
    /// </summary>
    public Money ListPriceFor(string? variant) => FindVariant(variant)?.Price ?? Price;

    // --- Stock ------------------------------------------------------------------------

    /// <summary>
    /// Units available for a selection. Preorders report int.MaxValue rather than a count:
    /// they are produced to demand, so "how many are left" is not a question with an answer.
    /// </summary>
    public int StockFor(string? variant)
    {
        if (IsPreorder)
        {
            return int.MaxValue;
        }

        return FindVariant(variant)?.Stock ?? Stock;
    }

    /// <summary>
    /// Whether this many can be sold right now. Checked at checkout so overselling is
    /// narrowed to orders paid in the same window, and again on acknowledge where the
    /// deduction actually happens.
    /// </summary>
    public bool CanFulfil(string? variant, int quantity) =>
        InStock && !IsPreorderClosed && StockFor(variant) >= quantity;

    /// <summary>
    /// Takes stock for a confirmed sale. Called when an officer acknowledges payment —
    /// the first moment the money is known to be real.
    /// </summary>
    public void DeductStock(string? variant, int quantity)
    {
        if (IsPreorder)
        {
            return; // Nothing to deduct; production follows demand.
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        var found = FindVariant(variant);

        if (found is not null)
        {
            found.Deduct(quantity);
        }
        else
        {
            if (quantity > Stock)
            {
                throw new DomainException($"Only {Stock} of '{Name}' left.");
            }

            Stock -= quantity;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Puts units back. Used both when an officer receives new supply and when a cancelled
    /// order returns what it had taken.
    /// </summary>
    public void Restock(string? variant, int quantity)
    {
        if (IsPreorder)
        {
            throw new DomainException($"{Name} is a preorder, so it has no stock to restock.");
        }

        var found = FindVariant(variant);

        if (found is not null)
        {
            found.Restock(quantity);
        }
        else
        {
            if (quantity <= 0)
            {
                throw new DomainException("Restock quantity must be at least 1.");
            }

            Stock += quantity;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVariantStock(int variantId, int stock)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        variant.SetStock(stock);
        UpdatedAt = DateTime.UtcNow;
    }

    // --- Selling mode -----------------------------------------------------------------

    public void SetSale(bool isOnSale, decimal percentage)
    {
        if (percentage is < 0m or > 100m)
        {
            throw new DomainException("A sale must be between 0% and 100%.");
        }

        IsOnSale = isOnSale;
        SalePercentage = decimal.Round(percentage, 2, MidpointRounding.ToEven);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stock and preorder are modes, not independent switches: turning preorder on makes
    /// every stock count meaningless, so the two are set together and never contradict.
    /// </summary>
    public void SetPreorder(bool isPreorder, DateTime? closesAt)
    {
        if (!isPreorder && closesAt is not null)
        {
            throw new DomainException("Only a preorder can have a closing date.");
        }

        IsPreorder = isPreorder;
        PreorderClosesAt = closesAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Photos to show for a selection: the variant's own, or the item's when the variant
    /// has none of its own. A variant without photos should look like the item, not blank.
    /// </summary>
    public IReadOnlyList<string> PhotosFor(string? variant)
    {
        var found = FindVariant(variant);
        return found is { PhotoUrls.Count: > 0 } ? found.PhotoUrls : PhotoUrls;
    }

    /// <summary>
    /// The officer-facing shutter. Named apart from SetStock(int) on purpose: two
    /// overloads differing only by bool vs int is exactly how someone eventually writes
    /// SetStock(0) meaning "hide it" and empties the shelf instead.
    /// </summary>
    public void SetInStock(bool inStock)
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
