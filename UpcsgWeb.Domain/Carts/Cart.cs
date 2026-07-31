using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Carts;

/// <summary>
/// One open cart per guilder. Root of its own aggregate; it references merch by id and
/// knows nothing about orders â€” checkout is what bridges the two.
/// </summary>
public class Cart : AggregateRoot
{
    /// <summary>Guards against a fat-fingered quantity emptying the merch table.</summary>
    public const int MaxQuantityPerLine = 25;

    private readonly List<CartLine> _lines = [];

    private Cart() { } // EF

    private Cart(int userId)
    {
        if (userId <= 0)
        {
            throw new DomainException("A cart must belong to a user.");
        }

        UserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public int UserId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<CartLine> Lines => _lines.AsReadOnly();

    public bool IsEmpty => _lines.Count == 0;

    public int TotalItems => _lines.Sum(l => l.Quantity);

    public static Cart For(int userId) => new(userId);

    /// <summary>
    /// Adds to the cart, or tops up an existing line for the same item and variant.
    /// Takes the MerchItem so stock and variant validity are checked against the real
    /// thing rather than trusted from the request.
    /// </summary>
    public void AddItem(MerchItem item, string? variant, int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        if (!item.InStock)
        {
            throw new DomainException($"{item.Name} is out of stock.");
        }

        if (item.IsPreorderClosed)
        {
            throw new DomainException($"Preorders for {item.Name} have closed.");
        }

        if (variant is not null && !item.HasVariant(variant))
        {
            throw new DomainException($"{item.Name} has no variant '{variant}'.");
        }

        var existing = Find(item.Id, variant);
        var newQuantity = (existing?.Quantity ?? 0) + quantity;

        // Checked but NOT reserved. Reserving here would let an abandoned cart sit on the
        // last hoodie; the real deduction happens when payment is acknowledged. This just
        // narrows overselling to orders paid inside the same window.
        if (!item.CanFulfil(variant, newQuantity))
        {
            var left = item.StockFor(variant);
            throw new DomainException(left == 0
                ? $"{item.Name}{(variant is null ? "" : $" ({variant})")} is sold out."
                : $"Only {left} of {item.Name}{(variant is null ? "" : $" ({variant})")} left.");
        }

        if (newQuantity > MaxQuantityPerLine)
        {
            throw new DomainException($"You can order at most {MaxQuantityPerLine} of one item.");
        }

        if (existing is null)
        {
            _lines.Add(new CartLine(item.Id, variant, quantity));
        }
        else
        {
            existing.SetQuantity(newQuantity);
        }

        Touch();
    }

    /// <summary>Sets an absolute quantity. Zero removes the line.</summary>
    public void SetQuantity(int merchItemId, string? variant, int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Quantity cannot be negative.");
        }

        if (quantity > MaxQuantityPerLine)
        {
            throw new DomainException($"You can order at most {MaxQuantityPerLine} of one item.");
        }

        var line = Find(merchItemId, variant)
            ?? throw new DomainException("That item is not in your cart.");

        if (quantity == 0)
        {
            _lines.Remove(line);
        }
        else
        {
            line.SetQuantity(quantity);
        }

        Touch();
    }

    public void RemoveItem(int merchItemId, string? variant)
    {
        var line = Find(merchItemId, variant)
            ?? throw new DomainException("That item is not in your cart.");

        _lines.Remove(line);
        Touch();
    }

    /// <summary>Emptied by checkout once the order has taken ownership of the lines.</summary>
    public void Clear()
    {
        _lines.Clear();
        Touch();
    }

    private CartLine? Find(int merchItemId, string? variant) =>
        _lines.FirstOrDefault(l => l.MerchItemId == merchItemId && l.Variant == variant);

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
