using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// Turns a cart into an order.
///
/// A domain service rather than a method on either aggregate: it touches Cart, Order
/// and MerchItem, and putting it on Cart would mean Cart knows how to build Orders
/// (and vice versa). The operation belongs to the domain but to no single aggregate.
///
/// This is the moment prices stop being live and become snapshots.
/// </summary>
public static class CheckoutService
{
    public static Order Checkout(
        Cart cart,
        IReadOnlyDictionary<int, MerchItem> availableItems,
        string? note = null)
    {
        if (cart.IsEmpty)
        {
            throw new DomainException("Your cart is empty.");
        }

        var order = Order.Place(cart.UserId, note);

        foreach (var line in cart.Lines)
        {
            if (!availableItems.TryGetValue(line.MerchItemId, out var item))
            {
                // The item vanished between adding to cart and checking out.
                throw new DomainException("An item in your cart is no longer available. Please review your cart.");
            }

            // AddLine re-checks stock and variant, so anything that sold out while the
            // cart sat idle is caught here rather than becoming an unfillable order.
            order.AddLine(item, line.Variant, line.Quantity);
        }

        // The order now owns these lines. Leaving the cart populated would let a
        // double-submit produce a second order for goods already committed.
        cart.Clear();

        return order;
    }
}
