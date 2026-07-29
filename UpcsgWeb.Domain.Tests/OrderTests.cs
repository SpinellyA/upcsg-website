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

        item.UpdateDetails(item.Name, item.Description, Money.Of(1200m));

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

    // --- Variant pricing -------------------------------------------------------------

    [Fact]
    public void LineTakesTheVariantsPriceNotTheItemBase()
    {
        var order = Order.Place(UserId);
        order.AddLine(HoodieWithPricedSizes(), "XL", 1);

        // Base is 750; XL is 820. Charging the base here would sell the biggest size at
        // the smallest one's price.
        Assert.Equal(820m, order.Lines[0].UnitPrice.Amount);
    }

    [Fact]
    public void EachVariantIsPricedIndependentlyOnTheSameOrder()
    {
        var item = HoodieWithPricedSizes();
        var order = Order.Place(UserId);

        order.AddLine(item, "S", 1);   // 750
        order.AddLine(item, "XL", 1);  // 820

        Assert.Equal(1570m, order.Total.Amount);
    }

    [Fact]
    public void ItemWithNoVariantsFallsBackToItsBasePrice()
    {
        var item = MerchItem.Create("Sticker pack", "Vinyl", Money.Of(90m));
        AssignId(item, 99);

        var order = Order.Place(UserId);
        order.AddLine(item, null, 2);

        Assert.Equal(180m, order.Total.Amount);
    }
}

public class MerchPricingTests
{
    [Fact]
    public void PriceFromIsTheCheapestVariant()
    {
        Assert.Equal(750m, HoodieWithPricedSizes().PriceFrom.Amount);
    }

    [Fact]
    public void PriceFromFallsBackToBaseWhenThereAreNoVariants()
    {
        var item = MerchItem.Create("Lanyard", "Woven", Money.Of(120m));
        Assert.Equal(120m, item.PriceFrom.Amount);
    }

    [Fact]
    public void HasPriceRangeOnlyWhenVariantsActuallyDiffer()
    {
        Assert.True(HoodieWithPricedSizes().HasPriceRange);

        // Hoodie() gives every size the same price, so there is no range to advertise.
        Assert.False(Hoodie().HasPriceRange);
    }

    [Fact]
    public void DuplicateVariantNamesAreRejected()
    {
        var item = MerchItem.Create("Tee", "Cotton", Money.Of(450m));
        item.AddVariant("M", string.Empty, Money.Of(450m));

        // Cart and order lines match variants by name, so a duplicate would make an
        // existing line ambiguous about which variant it meant.
        var ex = Assert.Throws<DomainException>(
            () => item.AddVariant("m", string.Empty, Money.Of(500m)));

        Assert.Contains("already has a variant", ex.Message);
    }

    [Fact]
    public void PhotosFallBackToTheItemWhenAVariantHasNone()
    {
        var item = MerchItem.Create("Tee", "Cotton", Money.Of(450m));
        item.ReplacePhotos(["item-front.jpg", "item-back.jpg"]);
        item.AddVariant("Red", string.Empty, Money.Of(450m), ["red.jpg"]);
        item.AddVariant("Blue", string.Empty, Money.Of(450m));

        Assert.Equal(["red.jpg"], item.PhotosFor("Red"));

        // A variant without its own photos should look like the item, not blank.
        Assert.Equal(["item-front.jpg", "item-back.jpg"], item.PhotosFor("Blue"));
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
