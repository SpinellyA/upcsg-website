using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Orders;

public static class CheckoutService
{
    public static Order Checkout(
        Cart cart,
        IReadOnlyDictionary<Guid, MerchItem> availableItems,
        PaymentMethod paymentMethod = PaymentMethod.GCash,
        string? note = null)
    {
        if (cart.IsEmpty)
        {
            throw new DomainException("Your cart is empty.");
        }

        // Availability is re-checked here, against the items as they are right now, not as
        // they were when they went in the cart. A cart can sit for days, and in that time an
        // item can sell out, be taken off sale, or have its preorder window close. Every
        // problem is collected rather than thrown on the first one, so the guilder fixes the
        // cart in one pass instead of discovering the next fault on each retry.
        var problems = new List<string>();

        foreach (var line in cart.Lines)
        {
            if (!availableItems.TryGetValue(line.MerchItemId, out var item))
            {
                // The cart line holds only an id, so a deleted item cannot be named here.
                problems.Add("An item in your cart is no longer available.");
                continue;
            }

            if (line.Variant is not null && !item.HasVariant(line.Variant))
            {
                problems.Add($"{item.Name} no longer comes in '{line.Variant}'.");
                continue;
            }

            if (!item.CanFulfil(line.Variant, line.Quantity))
            {
                problems.Add(item.ShortfallMessage(line.Variant));
            }
        }

        if (problems.Count > 0)
        {
            throw new DomainException(
                "Your cart has changed since you added these. "
                + string.Join(" ", problems)
                + " Please update your cart and try again.");
        }

        var order = Order.Create(cart.UserId, paymentMethod, note);

        foreach (var line in cart.Lines)
        {
            order.AddLine(availableItems[line.MerchItemId], line.Variant, line.Quantity);
        }

        // Cash goes to the officers now that the lines are on it. Online payment stays with
        // the guilder until they send a reference.
        if (paymentMethod == PaymentMethod.Cash)
        {
            order.QueueForCashPayment();
        }

        cart.Clear();

        return order;
    }
}
