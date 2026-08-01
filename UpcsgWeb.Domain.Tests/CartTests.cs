using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.ValueObjects;
using static UpcsgWeb.Domain.Tests.TestData;

namespace UpcsgWeb.Domain.Tests;

public class CartTests
{
    [Fact]
    public void NewCartIsEmpty()
    {
        var cart = Cart.For(UserId);
        Assert.True(cart.IsEmpty);
        Assert.Equal(0, cart.TotalItems);
    }

    [Fact]
    public void AddingSameItemAndVariantTopsUpTheLine()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();

        cart.AddItem(hoodie, "M", 1);
        cart.AddItem(hoodie, "M", 2);

        Assert.Single(cart.Lines);
        Assert.Equal(3, cart.Lines[0].Quantity);
    }

    [Fact]
    public void DifferentVariantsAreSeparateLines()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();

        cart.AddItem(hoodie, "M", 1);
        cart.AddItem(hoodie, "L", 1);

        Assert.Equal(2, cart.Lines.Count);
        Assert.Equal(2, cart.TotalItems);
    }

    [Fact]
    public void RejectsOutOfStockItem()
    {
        var cart = Cart.For(UserId);
        Assert.Throws<DomainException>(() => cart.AddItem(Hoodie(inStock: false), "M", 1));
    }

    [Fact]
    public void RejectsUnknownVariant()
    {
        var cart = Cart.For(UserId);
        Assert.Throws<DomainException>(() => cart.AddItem(Hoodie(), "XXL", 1));
    }

    [Fact]
    public void RejectsNonPositiveQuantity()
    {
        var cart = Cart.For(UserId);
        Assert.Throws<DomainException>(() => cart.AddItem(Hoodie(), "M", 0));
    }

    [Fact]
    public void EnforcesPerLineCap()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();

        Assert.Throws<DomainException>(() =>
            cart.AddItem(hoodie, "M", Cart.MaxQuantityPerLine + 1));
    }

    [Fact]
    public void CapAppliesToTheRunningTotalNotJustOneCall()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();

        cart.AddItem(hoodie, "M", Cart.MaxQuantityPerLine);

        Assert.Throws<DomainException>(() => cart.AddItem(hoodie, "M", 1));
    }

    [Fact]
    public void SettingQuantityToZeroRemovesTheLine()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 3);

        cart.SetQuantity(hoodie.Id, "M", 0);

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void UpdatingAnAbsentLineFails()
    {
        var cart = Cart.For(UserId);
        Assert.Throws<DomainException>(() => cart.SetQuantity(99, null, 1));
    }

    [Fact]
    public void CartHoldsNoPrices()
    {
        // Carts price live; only checkout snapshots. CartLine having no price field is
        // the guarantee, so assert the type surface rather than a value.
        var priceProperties = typeof(CartLine)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(Money));

        Assert.Empty(priceProperties);
    }
}

public class CheckoutServiceTests
{
    [Fact]
    public void CheckoutProducesAnOrderAwaitingPayment()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 2);

        var order = CheckoutService.Checkout(cart, Catalog(hoodie));

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Equal(UserId, order.UserId);
        Assert.Single(order.Lines);
        Assert.Equal(2, order.Lines[0].Quantity);
    }

    [Fact]
    public void CheckoutEmptiesTheCart()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);

        CheckoutService.Checkout(cart, Catalog(hoodie));

        // Otherwise a double-submit would order the same goods twice.
        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void CheckoutSnapshotsThePriceAtThatMoment()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie(price: 750m);
        cart.AddItem(hoodie, "M", 1);

        var order = CheckoutService.Checkout(cart, Catalog(hoodie));
        hoodie.UpdateDetails(hoodie.Name, hoodie.Description, Money.Of(1200m));

        Assert.Equal(750m, order.Total.Amount);
    }

    [Fact]
    public void EmptyCartCannotBeCheckedOut()
    {
        var cart = Cart.For(UserId);
        Assert.Throws<DomainException>(() => CheckoutService.Checkout(cart, Catalog()));
    }

    [Fact]
    public void ItemThatSoldOutWhileInTheCartBlocksCheckout()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);

        hoodie.SetInStock(false);

        var ex = Assert.Throws<DomainException>(() => CheckoutService.Checkout(cart, Catalog(hoodie)));
        Assert.Contains("out of stock", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ItemDeletedWhileInTheCartBlocksCheckout()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);

        // Catalog no longer contains it.
        var ex = Assert.Throws<DomainException>(() => CheckoutService.Checkout(cart, Catalog()));
        Assert.Contains("no longer available", ex.Message);
    }

    [Fact]
    public void FailedCheckoutLeavesTheCartIntact()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);
        hoodie.SetInStock(false);

        Assert.Throws<DomainException>(() => CheckoutService.Checkout(cart, Catalog(hoodie)));

        // The guilder must not lose their cart because one item lapsed.
        Assert.False(cart.IsEmpty);
    }

    [Fact]
    public void MultipleLinesCarryThroughWithCorrectTotal()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie(price: 750m);
        var tote = Tote(price: 250m);

        cart.AddItem(hoodie, "M", 2);
        cart.AddItem(tote, "One size", 1);

        var order = CheckoutService.Checkout(cart, Catalog(hoodie, tote));

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(1750m, order.Total.Amount);
    }

    [Fact]
    public void EndToEnd_CartToReceivedOrder()
    {
        var cart = Cart.For(UserId);
        var hoodie = Hoodie();
        cart.AddItem(hoodie, "M", 1);

        var order = CheckoutService.Checkout(cart, Catalog(hoodie));
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        var stockBefore = hoodie.StockFor("M");
        order.Acknowledge(Catalog(hoodie));
        order.Release();
        order.MarkReceived();

        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.True(cart.IsEmpty);
        Assert.NotNull(order.Receipt);

        // Acknowledging is what moves stock, not checkout.
        Assert.Equal(stockBefore - 1, hoodie.StockFor("M"));
    }
}
