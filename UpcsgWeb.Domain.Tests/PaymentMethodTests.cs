using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using static UpcsgWeb.Domain.Tests.TestData;

namespace UpcsgWeb.Domain.Tests;

public class PaymentMethodTests
{
    private static PaymentReceipt Reference() =>
        PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890");

    private static Order OnlineOrder(out MerchItem item, int stock = 10, int quantity = 2)
    {
        item = Hoodie(stock: stock);
        var order = Order.Create(UserId, PaymentMethod.GCash);
        order.AddLine(item, "M", quantity);
        return order;
    }

    private static Order CashOrder(out MerchItem item, int stock = 10, int quantity = 2)
    {
        item = Hoodie(stock: stock);
        var order = Order.Create(UserId, PaymentMethod.Cash);
        order.AddLine(item, "M", quantity);
        order.QueueForCashPayment();
        return order;
    }

    private static int StockOf(MerchItem item, string variant) =>
        item.Variants.Single(v => v.NameMatches(variant)).Stock;

    [Fact]
    public void AnOnlineOrderWaitsOnTheGuilderToSendAReference()
    {
        var order = OnlineOrder(out _);

        Assert.Equal(PaymentMethod.GCash, order.PaymentMethod);
        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.True(order.AwaitsPayment);
    }

    // Nothing for the guilder to submit, so it goes straight to the officers to be paid in
    // person and recorded.
    [Fact]
    public void ACashOrderGoesStraightIntoTheOfficerQueue()
    {
        var order = CashOrder(out _);

        Assert.Equal(PaymentMethod.Cash, order.PaymentMethod);
        Assert.True(order.IsCash);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void SubmittingAReferenceConfirmsAnOnlineOrderAndTakesTheStock()
    {
        var order = OnlineOrder(out var item, stock: 10, quantity: 2);
        Assert.Equal(10, StockOf(item, "M"));

        order.SubmitReceipt(Reference());
        var shortfall = order.ConfirmOnlinePaymentUnchecked(Catalog(item));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Empty(shortfall);
        Assert.Equal(8, StockOf(item, "M"));
        Assert.Equal(order.Total, order.AmountPaid);
    }

    [Fact]
    public void ACashOrderHasNoReferenceToSubmit()
    {
        var order = CashOrder(out _);

        var ex = Assert.Throws<DomainException>(() => order.SubmitReceipt(Reference()));
        Assert.Contains("cash order", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ACashOrderIsNeverConfirmedAutomatically()
    {
        var order = CashOrder(out var item);

        Assert.Throws<DomainException>(() => order.ConfirmOnlinePaymentUnchecked(Catalog(item)));
    }

    // The officer collecting the money is what commits a cash order, and only then.
    [Fact]
    public void ACashOrderTakesStockWhenAnOfficerRecordsThePayment()
    {
        var order = CashOrder(out var item, stock: 10, quantity: 2);

        Assert.Equal(10, StockOf(item, "M"));

        order.Acknowledge(Catalog(item));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Equal(8, StockOf(item, "M"));
    }

    // This is the priority rule in practice. A cash order holds nothing while it waits, so an
    // online order placed later takes the last of the stock first.
    [Fact]
    public void AnOnlineOrderTakesStockAheadOfACashOrderPlacedEarlier()
    {
        var item = Hoodie(stock: 2);

        var cash = Order.Create(UserId, PaymentMethod.Cash);
        cash.AddLine(item, "M", 2);
        cash.QueueForCashPayment();

        // Still waiting on an officer, so nothing has come off the shelf yet.
        Assert.Equal(2, StockOf(item, "M"));

        var online = Order.Create(Guid.CreateVersion7(), PaymentMethod.GCash);
        online.AddLine(item, "M", 2);
        online.SubmitReceipt(Reference());
        online.ConfirmOnlinePaymentUnchecked(Catalog(item));

        Assert.Equal(0, StockOf(item, "M"));

        // The cash order can no longer be filled, which the officer finds out before taking
        // any money rather than after.
        Assert.False(cash.CanFillAnything(Catalog(item)));
        Assert.Throws<DomainException>(() => cash.Acknowledge(Catalog(item)));
    }

    [Fact]
    public void AnOnlineOrderShortOnStockIsStillConfirmedAndOwesARefund()
    {
        var item = Hoodie(stock: 5);
        var order = Order.Create(UserId, PaymentMethod.GCash);
        order.AddLine(item, "M", 2);
        order.AddLine(item, "L", 2);

        // The M line can no longer be filled; L still can.
        var m = item.Variants.Single(v => v.NameMatches("M"));
        item.SetVariantStock(m.Id, 0);

        order.SubmitReceipt(Reference());
        var shortfall = order.ConfirmOnlinePaymentUnchecked(Catalog(item));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Single(shortfall);
        Assert.True(order.HasRefundDue);
    }

    // The guilder has already paid by this point, so the reference must not be thrown away.
    // The order waits for an officer to cancel and refund it in full instead.
    [Fact]
    public void AnOnlineOrderThatCannotBeFilledAtAllStaysInTheQueue()
    {
        var order = OnlineOrder(out var item, stock: 2, quantity: 2);

        var m = item.Variants.Single(v => v.NameMatches("M"));
        item.SetVariantStock(m.Id, 0);

        order.SubmitReceipt(Reference());
        var shortfall = order.ConfirmOnlinePaymentUnchecked(Catalog(item));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(shortfall);
        Assert.NotNull(order.Receipt);
    }

    // How an officer deals with a reference that turns out to be bad: cancelling an already
    // confirmed order puts the stock back.
    [Fact]
    public void CancellingAConfirmedOnlineOrderPutsTheStockBack()
    {
        var order = OnlineOrder(out var item, stock: 10, quantity: 2);

        order.SubmitReceipt(Reference());
        order.ConfirmOnlinePaymentUnchecked(Catalog(item));
        Assert.Equal(8, StockOf(item, "M"));

        order.Cancel("The GCash reference does not match any payment.", Catalog(item));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, StockOf(item, "M"));
    }

    // Nothing was ever committed, so cancelling gives nothing back.
    [Fact]
    public void CancellingAnUnpaidCashOrderReturnsNothing()
    {
        var order = CashOrder(out var item, stock: 10, quantity: 2);

        var returned = order.Cancel("The guilder never came to pay.", Catalog(item));

        Assert.Empty(returned);
        Assert.Equal(10, StockOf(item, "M"));
    }

    [Fact]
    public void CheckoutCarriesThePaymentMethodSelectionOntoTheOrder()
    {
        var item = Hoodie(stock: 5);

        var cart = Cart.Create(UserId);
        cart.AddItem(item, "M", 1);

        var order = CheckoutService.Checkout(cart, Catalog(item), PaymentMethod.Cash);

        Assert.Equal(PaymentMethod.Cash, order.PaymentMethod);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }
}
