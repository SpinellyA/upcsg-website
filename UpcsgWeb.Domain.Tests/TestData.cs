using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Tests;

internal static class TestData
{
    public static readonly Guid UserId = Guid.CreateVersion7();

    // There used to be an AssignId helper here that reflected over Entity.Id to fake a
    // database-assigned key, because Cart.AddItem and Order.AddLine both reject an item
    // with no id. Create assigns the id now, so the reflection is gone and these builders
    // produce objects that are already valid to reference.

    /// <summary>
    /// All three sizes at the same price, so tests that don't care about variant pricing
    /// keep reading the way they did. <see cref="HoodieWithPricedSizes"/> covers the case
    /// where they differ.
    ///
    /// Stocked generously by default so tests about the lifecycle aren't accidentally
    /// testing stock. Pass a smaller number to exercise running out.
    /// </summary>
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

    /// <summary>Sizes that cost different amounts — the case PriceFor has to get right.</summary>
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

    /// <summary>An order sitting in the officers' queue, receipt already submitted.</summary>
    public static Order PendingOrder(out MerchItem item)
    {
        item = Hoodie();
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));
        return order;
    }
}
