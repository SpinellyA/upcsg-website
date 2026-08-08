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

            var available = item is not null
                && item.InStock
                && (line.Variant is null || item.HasVariant(line.Variant));

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
