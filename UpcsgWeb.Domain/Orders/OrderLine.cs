using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// A single item within an order. An entity, not an aggregate root — it has no life
/// outside its Order and is only ever reached through it.
///
/// The name and unit price are SNAPSHOTS taken when the line was added, not lookups
/// against MerchItem. If they were lookups, repricing the hoodie would retroactively
/// rewrite what every past customer was charged.
/// </summary>
public class OrderLine : Entity
{
    private OrderLine() { } // EF

    internal OrderLine(int merchItemId, string itemName, string? variant, Money unitPrice, int quantity)
    {
        if (merchItemId <= 0)
        {
            throw new DomainException("An order line must reference a merch item.");
        }

        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new DomainException("An order line must record the item name.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        MerchItemId = merchItemId;
        ItemName = itemName;
        Variant = variant;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public int OrderId { get; private set; }

    /// <summary>Reference to the MerchItem aggregate by id only.</summary>
    public int MerchItemId { get; private set; }

    public string ItemName { get; private set; } = string.Empty;
    public string? Variant { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.MultiplyBy(Quantity);

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        Quantity = quantity;
    }
}
