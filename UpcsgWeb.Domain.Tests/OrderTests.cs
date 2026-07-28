using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.ValueObjects;
using static UpcsgWeb.Domain.Tests.TestData;

namespace UpcsgWeb.Domain.Tests;

public class OrderLifecycleTests
{
    private static Order AwaitingPaymentOrder(out MerchItem item)
    {
        item = Hoodie();
        var order = Order.Place(UserId);
        order.AddLine(item, "M", 1);
        return order;
    }

    private static PaymentReceipt Receipt() => PaymentReceipt.Submit("0001234567890", null);

    // --- Initial state ---------------------------------------------------------------

    [Fact]
    public void CheckoutDoesNotMeanPaid_OrderStartsAwaitingPayment()
    {
        var order = Order.Place(UserId);
        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.True(order.AwaitsPayment);
        Assert.Null(order.Receipt);
    }

    [Fact]
    public void FullFlow_AwaitingPaymentToReceived()
    {
        var order = AwaitingPaymentOrder(out _);

        order.SubmitReceipt(Receipt());
        Assert.Equal(OrderStatus.Pending, order.Status);

        order.Acknowledge();
        Assert.Equal(OrderStatus.Acknowledged, order.Status);

        order.Release();
        Assert.Equal(OrderStatus.Released, order.Status);

        order.MarkReceived();
        Assert.Equal(OrderStatus.Received, order.Status);
    }

    // --- The payment gate ------------------------------------------------------------

    [Fact]
    public void OfficerCannotAcknowledgeBeforeAReceiptArrives()
    {
        var order = AwaitingPaymentOrder(out _);

        // This is the whole point of the AwaitingPayment stage.
        var ex = Assert.Throws<DomainException>(order.Acknowledge);
        Assert.Contains("AwaitingPayment", ex.Message);
    }

    [Fact]
    public void CannotSubmitReceiptTwice()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());

        // Prevents swapping the proof after an officer has started looking at it.
        Assert.Throws<DomainException>(() => order.SubmitReceipt(Receipt()));
    }

    [Fact]
    public void SubmittingReceiptRecordsIt()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(PaymentReceipt.Submit("  0009876543210  ", "https://cdn/x.png"));

        Assert.NotNull(order.Receipt);
        Assert.Equal("0009876543210", order.Receipt!.ReferenceNumber);
        Assert.Equal("https://cdn/x.png", order.Receipt.ScreenshotUrl);
    }

    [Fact]
    public void ReceiptRequiresAReferenceNumber() =>
        Assert.Throws<DomainException>(() => PaymentReceipt.Submit("   ", null));

    [Fact]
    public void EmptyOrderCannotBePaidFor()
    {
        var order = Order.Place(UserId);
        Assert.Throws<DomainException>(() => order.SubmitReceipt(Receipt()));
    }

    // --- Receipt rejection -----------------------------------------------------------

    [Fact]
    public void RejectingReceiptSendsItBackToTheGuilder()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());

        order.RejectReceipt("Amount does not match the order total");

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Null(order.Receipt);
        Assert.Equal("Amount does not match the order total", order.ReceiptRejectionReason);
    }

    [Fact]
    public void GuilderCanResubmitAfterRejection()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());
        order.RejectReceipt("Unreadable screenshot");

        order.SubmitReceipt(PaymentReceipt.Submit("0001111111111", null));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.ReceiptRejectionReason);
    }

    [Fact]
    public void RejectionRequiresAReason()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());

        Assert.Throws<DomainException>(() => order.RejectReceipt(" "));
    }

    [Fact]
    public void CannotRejectAReceiptThatWasNeverSubmitted()
    {
        var order = AwaitingPaymentOrder(out _);
        Assert.Throws<DomainException>(() => order.RejectReceipt("nope"));
    }

    [Fact]
    public void CannotRejectOnceAcknowledged()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());
        order.Acknowledge();

        Assert.Throws<DomainException>(() => order.RejectReceipt("too late"));
    }

    // --- Illegal transitions ---------------------------------------------------------

    [Fact]
    public void CannotSkipAcknowledged()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());

        Assert.Throws<DomainException>(order.Release);
    }

    [Fact]
    public void CannotSkipReleased()
    {
        var order = AwaitingPaymentOrder(out _);
        order.SubmitReceipt(Receipt());
        order.Acknowledge();

        Assert.Throws<DomainException>(order.MarkReceived);
    }

    [Fact]
    public void CannotGoBackwards()
    {
        var order = PendingOrder(out _);
        order.Acknowledge();
        order.Release();

        Assert.Throws<DomainException>(order.Acknowledge);
    }

    [Fact]
    public void ReceivedIsTerminal()
    {
        var order = PendingOrder(out _);
        order.Acknowledge();
        order.Release();
        order.MarkReceived();

        Assert.Throws<DomainException>(order.Release);
        Assert.Throws<DomainException>(() => order.Cancel("changed my mind"));
    }

    [Fact]
    public void CannotCancelAfterRelease()
    {
        var order = PendingOrder(out _);
        order.Acknowledge();
        order.Release();

        Assert.Throws<DomainException>(() => order.Cancel("too late"));
    }

    [Fact]
    public void UnpaidOrderCanBeCancelled()
    {
        var order = AwaitingPaymentOrder(out _);
        order.Cancel("Never paid");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Never paid", order.CancellationReason);
    }

    [Fact]
    public void CancellingRequiresAReason()
    {
        var order = AwaitingPaymentOrder(out _);
        Assert.Throws<DomainException>(() => order.Cancel("  "));
    }

    // --- Editing ---------------------------------------------------------------------

    [Fact]
    public void LinesFreezeOnceAReceiptIsSubmitted()
    {
        var order = AwaitingPaymentOrder(out var item);
        Assert.True(order.IsEditable);

        order.SubmitReceipt(Receipt());

        Assert.False(order.IsEditable);
        Assert.Throws<DomainException>(() => order.AddLine(item, "L", 1));
    }

    // --- Pricing ---------------------------------------------------------------------

    [Fact]
    public void RepricingMerchDoesNotRewriteExistingOrders()
    {
        var order = AwaitingPaymentOrder(out var item);
        Assert.Equal(750m, order.Total.Amount);

        item.UpdateDetails(item.Name, item.Description, Money.Of(1200m), item.ImageUrl);

        Assert.Equal(750m, order.Total.Amount);
        Assert.Equal(750m, order.Lines[0].UnitPrice.Amount);
    }

    [Fact]
    public void TotalSumsLines()
    {
        var order = Order.Place(UserId);
        order.AddLine(Hoodie(), "M", 2);       // 1500
        order.AddLine(Tote(), "One size", 1);  //  250

        Assert.Equal(1750m, order.Total.Amount);
    }
}

public class MoneyTests
{
    [Fact]
    public void RejectsNegativeAmounts() =>
        Assert.Throws<DomainException>(() => Money.Of(-1m));

    [Fact]
    public void RoundsToTwoDecimals() =>
        Assert.Equal(10.12m, Money.Of(10.124m).Amount);

    [Fact]
    public void RefusesToMixCurrencies() =>
        Assert.Throws<DomainException>(() => Money.Of(100m, "PHP").Add(Money.Of(100m, "USD")));

    [Fact]
    public void EqualityIsByValue() =>
        Assert.Equal(Money.Of(750m), Money.Of(750m));
}
