using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Mapping;

public static class CartMapping
{
    /// <summary>
    /// Builds the cart view by resolving each line against the merch table right now.
    /// Prices are read live on purpose — a cart is not a price lock, and showing a
    /// stale figure here would mean the checkout total silently disagrees with it.
    /// </summary>
    public static CartDto ToDto(this Cart cart, IReadOnlyDictionary<int, MerchItem> items)
    {
        var lines = new List<CartLineDto>();

        foreach (var line in cart.Lines)
        {
            items.TryGetValue(line.MerchItemId, out var item);

            // Deleted or sold out since it was added: still shown, but flagged, so the
            // guilder sees why checkout is blocked instead of hitting a bare error.
            var available = item is not null
                && item.InStock
                && (line.Variant is null || item.HasVariant(line.Variant));

            // PriceFor, so a cart line for the dearest size shows and charges that size's
            // price rather than the item's base.
            var unitPrice = item?.PriceFor(line.Variant).Amount ?? 0m;

            lines.Add(new CartLineDto
            {
                MerchItemId = line.MerchItemId,
                ItemName = item?.Name ?? "Unavailable item",
                Variant = line.Variant,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * line.Quantity,

                // The variant's photo when it has one, so the cart row matches what was
                // chosen on the detail page.
                ImageUrl = item?.PhotosFor(line.Variant).FirstOrDefault(),
                Available = available,
            });
        }

        return new CartDto
        {
            Lines = lines,
            TotalItems = cart.TotalItems,
            Total = lines.Where(l => l.Available).Sum(l => l.LineTotal),
            CanCheckout = lines.Count > 0 && lines.All(l => l.Available),
        };
    }
}
