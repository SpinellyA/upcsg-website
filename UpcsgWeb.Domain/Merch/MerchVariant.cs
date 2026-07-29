using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Merch;

/// <summary>
/// One purchasable option of a <see cref="MerchItem"/> — a size, a colourway, a bundle.
///
/// An entity rather than the bare string it used to be, because a variant now carries its
/// own price and photos. It is NOT an aggregate root: variants have no meaning apart from
/// their item, and are only ever reached through it.
///
/// Cart and order lines still reference a variant by NAME, not by id. That is deliberate —
/// those lines are snapshots of what was bought, and must keep reading correctly even if
/// the variant is later renamed, repriced or deleted.
/// </summary>
public class MerchVariant : Entity
{
    private readonly List<string> _photoUrls = [];

    private MerchVariant() { } // EF

    public int MerchItemId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>What this specific option costs, independent of the item's base price.</summary>
    public Money Price { get; private set; } = Money.Zero();

    /// <summary>Ordering is meaningful: the first photo is the one the gallery opens on.</summary>
    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    public int DisplayOrder { get; private set; }

    internal static MerchVariant Create(string name, string description, Money price, int displayOrder)
    {
        var variant = new MerchVariant();
        variant.Update(name, description, price);
        variant.DisplayOrder = displayOrder;
        return variant;
    }

    internal void Update(string name, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A variant needs a name.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
    }

    internal void SetDisplayOrder(int order) => DisplayOrder = order;

    internal void ReplacePhotos(IEnumerable<string> photoUrls)
    {
        _photoUrls.Clear();

        foreach (var url in photoUrls ?? [])
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                _photoUrls.Add(url.Trim());
            }
        }
    }

    public bool NameMatches(string? candidate) =>
        candidate is not null && string.Equals(Name, candidate.Trim(), StringComparison.OrdinalIgnoreCase);
}
