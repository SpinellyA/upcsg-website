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

    internal OrderLine(Guid merchItemId, string itemName, string? variant, Money unitPrice, int quantity)
    {
        if (merchItemId == Guid.Empty)
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

        Id = Guid.CreateVersion7();
        MerchItemId = merchItemId;
        ItemName = itemName;
        Variant = variant;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid OrderId { get; private set; }

    /// <summary>Reference to the MerchItem aggregate by id only.</summary>
    public Guid MerchItemId { get; private set; }

    public string ItemName { get; private set; } = string.Empty;
    public string? Variant { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.MultiplyBy(Quantity);

    public OrderLineStatus Status { get; private set; } = OrderLineStatus.ToFulfil;

    /// <summary>Why this line couldn't be filled. Shown to the guilder verbatim.</summary>
    public string? ShortfallReason { get; private set; }

    public bool IsRefundDue => Status == OrderLineStatus.RefundDue;

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be at least 1.");
        }

        Quantity = quantity;
    }

    internal void MarkRefundDue(string reason)
    {
        if (Status == OrderLineStatus.Refunded)
        {
            throw new DomainException($"{ItemName} has already been refunded.");
        }

        Status = OrderLineStatus.RefundDue;
        ShortfallReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// A restock arrived and this line can be filled after all. Refused once the money has
    /// gone back — you cannot un-send GCash.
    /// </summary>
    internal void RestoreToFulfil()
    {
        if (Status == OrderLineStatus.Refunded)
        {
            throw new DomainException(
                $"{ItemName} was already refunded, so it can't be put back on this order. "
                + "Ask the guilder to order it again.");
        }

        Status = OrderLineStatus.ToFulfil;
        ShortfallReason = null;
    }

    internal void MarkRefunded()
    {
        if (Status != OrderLineStatus.RefundDue)
        {
            throw new DomainException($"{ItemName} is not awaiting a refund.");
        }

        Status = OrderLineStatus.Refunded;
    }
}
