using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Orders;

public static class CheckoutService
{
    public static Order Checkout(
        Cart cart,
        IReadOnlyDictionary<Guid, MerchItem> availableItems,
        string? note = null)
    {
        if (cart.IsEmpty)
        {
            throw new DomainException("Your cart is empty.");
        }

        var order = Order.Create(cart.UserId, note);

        foreach (var line in cart.Lines)
        {
            if (!availableItems.TryGetValue(line.MerchItemId, out var item))
            {
                throw new DomainException("An item in your cart is no longer available. Please review your cart.");
            }

            order.AddLine(item, line.Variant, line.Quantity);
        }

        cart.Clear();

        return order;
    }
}
