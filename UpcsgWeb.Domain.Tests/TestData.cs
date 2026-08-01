using System.Reflection;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Tests;

internal static class TestData
{
    public const int UserId = 42;

    /// <summary>
    /// Ids are database-assigned, so tests set them directly to simulate a persisted
    /// item — Cart.AddItem and Order.AddLine both reject an item with no id.
    /// </summary>
    public static void AssignId(Entity entity, int id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!
            .SetValue(entity, id, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

    /// <summary>
    /// All three sizes at the same price, so tests that don't care about variant pricing
    /// keep reading the way they did. <see cref="HoodieWithPricedSizes"/> covers the case
    /// where they differ.
    /// </summary>
    /// <summary>
    /// Stocked generously by default so tests about the lifecycle aren't accidentally
    /// testing stock. Pass a smaller number to exercise running out.
    /// </summary>
    public static MerchItem Hoodie(int id = 1, decimal price = 750m, bool inStock = true, int stock = 100)
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Midnight indigo pullover", Money.Of(price));
        AssignId(item, id);

        foreach (var (size, index) in new[] { "S", "M", "L" }.Select((s, i) => (s, i)))
        {
            var variant = item.AddVariant(size, string.Empty, Money.Of(price));
            AssignId(variant, index + 1);
            item.SetVariantStock(variant.Id, stock);
        }

        if (!inStock)
        {
            item.SetInStock(false);
        }

        return item;
    }

    /// <summary>Sizes that cost different amounts — the case PriceFor has to get right.</summary>
    public static MerchItem HoodieWithPricedSizes(int id = 1)
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Midnight indigo pullover", Money.Of(750m));
        AssignId(item, id);

        AssignId(item.AddVariant("S", string.Empty, Money.Of(750m)), 1);
        AssignId(item.AddVariant("L", string.Empty, Money.Of(780m)), 2);
        AssignId(item.AddVariant("XL", string.Empty, Money.Of(820m)), 3);

        return item;
    }

    public static MerchItem Tote(int id = 2, decimal price = 250m, int stock = 100)
    {
        var item = MerchItem.Create("Starlight Tote", "Canvas", Money.Of(price));
        AssignId(item, id);

        var variant = item.AddVariant("One size", string.Empty, Money.Of(price));
        AssignId(variant, 10);
        item.SetVariantStock(variant.Id, stock);

        return item;
    }

    public static IReadOnlyDictionary<int, MerchItem> Catalog(params MerchItem[] items) =>
        items.ToDictionary(i => i.Id);

    /// <summary>An order sitting in the officers' queue, receipt already submitted.</summary>
    public static Order PendingOrder(out MerchItem item)
    {
        item = Hoodie();
        var order = Order.Place(UserId);
        order.AddLine(item, "M", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));
        return order;
    }
}
