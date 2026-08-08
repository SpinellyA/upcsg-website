using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Carts;

public class Cart : AggregateRoot
{
    public const int MaxQuantityPerLine = 25;

    private readonly List<CartLine> _lines = [];

    private Cart() { }

    private Cart(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A cart must belong to a user.");
        }

        Id = Guid.CreateVersion7();
        UserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<CartLine> Lines => _lines.AsReadOnly();

    public bool IsEmpty => _lines.Count == 0;

    public int TotalItems => _lines.Sum(l => l.Quantity);

    public static Cart Create(Guid userId) => new(userId);

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

    public void SetQuantity(Guid merchItemId, string? variant, int quantity)
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

    public void RemoveItem(Guid merchItemId, string? variant)
    {
        var line = Find(merchItemId, variant)
            ?? throw new DomainException("That item is not in your cart.");

        _lines.Remove(line);
        Touch();
    }

    public void Clear()
    {
        _lines.Clear();
        Touch();
    }

    private CartLine? Find(Guid merchItemId, string? variant) =>
        _lines.FirstOrDefault(l => l.MerchItemId == merchItemId && l.Variant == variant);

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
