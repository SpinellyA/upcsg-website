using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using static UpcsgWeb.Domain.Tests.TestData;

namespace UpcsgWeb.Domain.Tests;

// A cart can sit for days. Everything here is about the gap between adding something and
// ordering it, during which stock runs out, an item is withdrawn, or a preorder window shuts.
public class CartRevalidationTests
{
    private static MerchItem SoldOutSince(MerchItem item, string variant)
    {
        var found = item.Variants.Single(v => v.NameMatches(variant));
        item.SetVariantStock(found.Id, 0);
        return item;
    }

    [Fact]
    public void RaisingTheQuantityPastWhatIsLeftIsRefused()
    {
        var hoodie = Hoodie(stock: 5);
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 2);

        var found = hoodie.Variants.Single(v => v.NameMatches("M"));
        hoodie.SetVariantStock(found.Id, 3);

        var ex = Assert.Throws<DomainException>(() => cart.SetQuantity(hoodie, "M", 4));
        Assert.Contains("Only 3", ex.Message);

        // The line is untouched by the refusal.
        Assert.Equal(2, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void DroppingTheQuantityToWhatIsLeftIsAllowed()
    {
        var hoodie = Hoodie(stock: 5);
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 5);

        var found = hoodie.Variants.Single(v => v.NameMatches("M"));
        hoodie.SetVariantStock(found.Id, 3);

        cart.SetQuantity(hoodie, "M", 3);

        Assert.Equal(3, cart.Lines.Single().Quantity);
    }

    // The one case that must never be blocked: if a guilder could not remove a line that had
    // gone unavailable, their cart would be permanently un-checkout-able and un-clearable.
    [Fact]
    public void ASoldOutLineCanStillBeRemoved()
    {
        var hoodie = Hoodie(stock: 2);
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 2);

        SoldOutSince(hoodie, "M");

        cart.SetQuantity(hoodie, "M", 0);

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void CheckoutRefusesAnItemThatSoldOutWhileInTheCart()
    {
        var hoodie = Hoodie(stock: 2);
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 2);

        SoldOutSince(hoodie, "M");

        var ex = Assert.Throws<DomainException>(
            () => CheckoutService.Checkout(cart, Catalog(hoodie)));

        Assert.Contains("sold out", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Nothing was taken: the cart survives so it can be corrected.
        Assert.False(cart.IsEmpty);
    }

    [Fact]
    public void CheckoutRefusesAnItemTakenOffSaleWhileInTheCart()
    {
        var hoodie = Hoodie();
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 1);

        hoodie.SetInStock(false);

        var ex = Assert.Throws<DomainException>(
            () => CheckoutService.Checkout(cart, Catalog(hoodie)));

        Assert.Contains("out of stock", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckoutRefusesAPreorderWhoseWindowClosedWhileInTheCart()
    {
        var tote = Tote();
        tote.SetPreorder(true, DateTime.UtcNow.AddDays(7));

        var cart = Cart.Create(UserId);
        cart.AddItem(tote, "One size", 3);

        tote.SetPreorder(true, DateTime.UtcNow.AddSeconds(-1));

        var ex = Assert.Throws<DomainException>(
            () => CheckoutService.Checkout(cart, Catalog(tote)));

        Assert.Contains("Preorders", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Reporting one fault at a time would make the guilder retry, fix, retry, fix.
    [Fact]
    public void CheckoutReportsEveryProblemAtOnce()
    {
        var hoodie = Hoodie(stock: 2);
        var tote = Tote(stock: 2);

        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 2);
        cart.AddItem(tote, "One size", 2);

        SoldOutSince(hoodie, "M");
        tote.SetInStock(false);

        var ex = Assert.Throws<DomainException>(
            () => CheckoutService.Checkout(cart, Catalog(hoodie, tote)));

        Assert.Contains("Cosmic Hoodie", ex.Message);
        Assert.Contains("Starlight Tote", ex.Message);
    }

    [Fact]
    public void CheckoutRefusesAnItemThatWasWithdrawnEntirely()
    {
        var hoodie = Hoodie();
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 1);

        // Catalog without it: the item was deleted after it went in the cart.
        var ex = Assert.Throws<DomainException>(
            () => CheckoutService.Checkout(cart, Catalog()));

        Assert.Contains("no longer available", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AStillValidCartCheckoutsAndClears()
    {
        var hoodie = Hoodie(stock: 5);
        var cart = Cart.Create(UserId);
        cart.AddItem(hoodie, "M", 2);

        var order = CheckoutService.Checkout(cart, Catalog(hoodie));

        Assert.Single(order.Lines);
        Assert.Equal(2, order.Lines.Single().Quantity);
        Assert.True(cart.IsEmpty);
    }
}
