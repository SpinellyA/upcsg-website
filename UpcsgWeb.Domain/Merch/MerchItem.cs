using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Merch;

/// <summary>A purchasable item. Root of its own aggregate; orders reference it by id.</summary>
public class MerchItem : AggregateRoot
{
    private readonly List<string> _variants = [];

    private MerchItem() { } // EF

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero();
    public string? ImageUrl { get; private set; }
    public bool InStock { get; private set; } = true;

    public IReadOnlyList<string> Variants => _variants.AsReadOnly();

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static MerchItem Create(
        string name,
        string description,
        Money price,
        IEnumerable<string>? variants = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Merch needs a name.");
        }

        var item = new MerchItem
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            ImageUrl = imageUrl,
        };

        foreach (var variant in variants ?? [])
        {
            item.AddVariant(variant);
        }

        return item;
    }

    public void AddVariant(string variant)
    {
        if (string.IsNullOrWhiteSpace(variant))
        {
            return;
        }

        var trimmed = variant.Trim();
        if (!_variants.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            _variants.Add(trimmed);
        }
    }

    public bool HasVariant(string variant) =>
        _variants.Contains(variant, StringComparer.OrdinalIgnoreCase);

    public void UpdateDetails(string name, string description, Money price, string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Merch needs a name.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;

        // Existing order lines keep their snapshotted price — repricing here is safe.
        Price = price;
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceVariants(IEnumerable<string> variants)
    {
        _variants.Clear();
        foreach (var variant in variants)
        {
            AddVariant(variant);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStock(bool inStock)
    {
        InStock = inStock;
        UpdatedAt = DateTime.UtcNow;
    }
}
