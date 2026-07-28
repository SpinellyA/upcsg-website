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

    public static MerchItem Hoodie(int id = 1, decimal price = 750m, bool inStock = true)
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Midnight indigo pullover",
            Money.Of(price), ["S", "M", "L"]);

        AssignId(item, id);

        if (!inStock)
        {
            item.SetStock(false);
        }

        return item;
    }

    public static MerchItem Tote(int id = 2, decimal price = 250m)
    {
        var item = MerchItem.Create("Starlight Tote", "Canvas", Money.Of(price), ["One size"]);
        AssignId(item, id);
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
        order.SubmitReceipt(PaymentReceipt.Submit("0001234567890", null));
        return order;
    }
}
