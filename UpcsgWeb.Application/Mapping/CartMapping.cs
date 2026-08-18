using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Mapping;

public static class CartMapping
{
    public static CartDto ToDto(this Cart cart, IReadOnlyDictionary<Guid, MerchItem> items)
    {
        var lines = new List<CartLineDto>();

        foreach (var line in cart.Lines)
        {
            items.TryGetValue(line.MerchItemId, out var item);

            // Judged against the quantity actually in the cart, not merely whether the item
            // exists. InStock alone used to pass a line whose stock had run down to fewer
            // than it asks for, or whose preorder window had shut, so the cart offered a
            // checkout the domain would then refuse.
            var variantGone = item is not null
                && line.Variant is not null
                && !item.HasVariant(line.Variant);

            var available = item is not null
                && !variantGone
                && item.CanFulfil(line.Variant, line.Quantity);

            string? reason = null;

            if (item is null)
            {
                reason = "This item is no longer available.";
            }
            else if (variantGone)
            {
                reason = $"{item.Name} no longer comes in '{line.Variant}'.";
            }
            else if (!available)
            {
                reason = item.ShortfallMessage(line.Variant);
            }

            // Meaningless for a preorder, which reports int.MaxValue rather than a count.
            int? stockLeft = item is null || item.IsPreorder
                ? null
                : item.StockFor(line.Variant);

            var unitPrice = item?.PriceFor(line.Variant).Amount ?? 0m;

            lines.Add(new CartLineDto
            {
                MerchItemId = line.MerchItemId,
                ItemName = item?.Name ?? "Unavailable item",
                Variant = line.Variant,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * line.Quantity,

                ImageUrl = item?.PhotosFor(line.Variant).FirstOrDefault(),
                Available = available,
                UnavailableReason = reason,
                StockLeft = stockLeft,
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
