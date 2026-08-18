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
            throw new DomainException(item.ShortfallMessage(variant));
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

    /// <summary>
    /// Changes how many of a line are wanted, re-checking availability as it goes. The item
    /// has to be passed in because stock can have moved since it went in the cart: without
    /// it this could raise a line above what is left, which is how a sold-out item used to
    /// reach checkout.
    /// </summary>
    public void SetQuantity(MerchItem item, string? variant, int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Quantity cannot be negative.");
        }

        if (quantity > MaxQuantityPerLine)
        {
            throw new DomainException($"You can order at most {MaxQuantityPerLine} of one item.");
        }

        var line = Find(item.Id, variant)
            ?? throw new DomainException("That item is not in your cart.");

        // Removing is always allowed. Refusing it would strand a guilder with a line they
        // cannot check out and cannot delete, which is the one state worth avoiding here.
        if (quantity == 0)
        {
            _lines.Remove(line);
            Touch();
            return;
        }

        if (!item.CanFulfil(variant, quantity))
        {
            throw new DomainException(item.ShortfallMessage(variant));
        }

        line.SetQuantity(quantity);
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
