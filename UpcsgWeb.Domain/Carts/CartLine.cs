using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Carts;

/// <summary>
/// A line in a cart. Note what it does NOT hold: a price.
///
/// A cart is a shopping intention, not a commitment, so it must always reflect the
/// current price. Snapshotting happens exactly once, at checkout, when the guilder
/// actually commits. Storing a price here would let a stale cart lock in an old one.
///
/// An Entity, not an AggregateRoot: it is only ever reached through its Cart, which is
/// why no repository can load one on its own.
/// </summary>
public class CartLine : Entity
{
    private CartLine() { } // EF

    internal CartLine(int merchItemId, string? variant, int quantity)
    {
        MerchItemId = merchItemId;
        Variant = variant;
        Quantity = quantity;
    }

    public int CartId { get; private set; }
    public int MerchItemId { get; private set; }
    public string? Variant { get; private set; }
    public int Quantity { get; private set; }

    internal void SetQuantity(int quantity) => Quantity = quantity;
}
