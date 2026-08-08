using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Merch;

public class MerchItem : AggregateRoot
{
    private readonly List<MerchVariant> _variants = [];
    private readonly List<string> _photoUrls = [];

    private MerchItem() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public Money Price { get; private set; } = Money.Zero();

    public bool InStock { get; private set; } = true;

    public decimal SalePercentage { get; private set; }

    public bool IsOnSale { get; private set; }

    public bool IsPreorder { get; private set; }

    public DateTime? PreorderClosesAt { get; private set; }

    public int Stock { get; private set; }

    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    public IReadOnlyList<MerchVariant> Variants =>
        _variants.OrderBy(v => v.DisplayOrder).ToList().AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public Money PriceFrom => ApplySale(ListPriceFrom);

    public Money ListPriceFrom => _variants.Count == 0
        ? Price
        : _variants.MinBy(v => v.Price.Amount)!.Price;

    public bool HasPriceRange =>
        _variants.Count > 1 && _variants.Select(v => v.Price.Amount).Distinct().Count() > 1;

    public bool HasActiveSale => IsOnSale && SalePercentage > 0m;

    public bool IsPreorderClosed =>
        IsPreorder && PreorderClosesAt is { } closes && closes <= DateTime.UtcNow;

    private Money ApplySale(Money list) =>
        HasActiveSale ? Money.Of(list.Amount * (1m - SalePercentage / 100m), list.Currency) : list;

    public static MerchItem Create(string name, string description, Money price)
    {
        var item = new MerchItem { Id = Guid.CreateVersion7() };
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

    public void UpdateVariant(Guid variantId, string name, string description, Money price, IEnumerable<string>? photoUrls)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        if (_variants.Any(v => v.Id != variantId && v.NameMatches(name)))
        {
            throw new DomainException($"{Name} already has a variant called '{name.Trim()}'.");
        }

        variant.Update(name, description, price);
        variant.ReplacePhotos(photoUrls ?? []);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVariant(Guid variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        _variants.Remove(variant);
        Resequence();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReorderVariants(IReadOnlyList<Guid> variantIdsInOrder)
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

    public Money PriceFor(string? variant) => ApplySale(ListPriceFor(variant));

    public Money ListPriceFor(string? variant) => FindVariant(variant)?.Price ?? Price;

    public int StockFor(string? variant)
    {
        if (IsPreorder)
        {
            return int.MaxValue;
        }

        return FindVariant(variant)?.Stock ?? Stock;
    }

    public bool CanFulfil(string? variant, int quantity) =>
        InStock && !IsPreorderClosed && StockFor(variant) >= quantity;

    public void DeductStock(string? variant, int quantity)
    {
        if (IsPreorder)
        {
            return;
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

    public void SetVariantStock(Guid variantId, int stock)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("That variant does not exist on this item.");

        variant.SetStock(stock);
        UpdatedAt = DateTime.UtcNow;
    }

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

    public IReadOnlyList<string> PhotosFor(string? variant)
    {
        var found = FindVariant(variant);
        return found is { PhotoUrls.Count: > 0 } ? found.PhotoUrls : PhotoUrls;
    }

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
