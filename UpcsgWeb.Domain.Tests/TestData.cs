using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Tests;

internal static class TestData
{
    public static readonly Guid UserId = Guid.CreateVersion7();

    public static MerchItem Hoodie(decimal price = 750m, bool inStock = true, int stock = 100)
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Midnight indigo pullover", Money.Of(price));

        foreach (var size in new[] { "S", "M", "L" })
        {
            var variant = item.AddVariant(size, string.Empty, Money.Of(price));
            item.SetVariantStock(variant.Id, stock);
        }

        if (!inStock)
        {
            item.SetInStock(false);
        }

        return item;
    }

    public static MerchItem HoodieWithPricedSizes()
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Midnight indigo pullover", Money.Of(750m));

        item.AddVariant("S", string.Empty, Money.Of(750m));
        item.AddVariant("L", string.Empty, Money.Of(780m));
        item.AddVariant("XL", string.Empty, Money.Of(820m));

        return item;
    }

    public static MerchItem Tote(decimal price = 250m, int stock = 100)
    {
        var item = MerchItem.Create("Starlight Tote", "Canvas", Money.Of(price));

        var variant = item.AddVariant("One size", string.Empty, Money.Of(price));
        item.SetVariantStock(variant.Id, stock);

        return item;
    }

    public static IReadOnlyDictionary<Guid, MerchItem> Catalog(params MerchItem[] items) =>
        items.ToDictionary(i => i.Id);

    public static Order PendingOrder(out MerchItem item)
    {
        item = Hoodie();
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));
        return order;
    }
}
