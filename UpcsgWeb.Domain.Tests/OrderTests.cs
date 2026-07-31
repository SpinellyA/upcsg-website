using UpcsgWeb.Domain.Carts;
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
        var order = AwaitingPaymentOrder(out var item);

        order.SubmitReceipt(Receipt());
        Assert.Equal(OrderStatus.Pending, order.Status);

        order.Acknowledge(Catalog(item));
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
        var order = AwaitingPaymentOrder(out var item);

        // This is the whole point of the AwaitingPayment stage.
        var ex = Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(item)));
        Assert.Contains("AwaitingPayment", ex.Message);
    }

    [Fact]
    public void CannotSubmitReceiptTwice()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());

        // Prevents swapping the proof after an officer has started looking at it.
        Assert.Throws<DomainException>(() => order.SubmitReceipt(Receipt()));
    }

    [Fact]
    public void SubmittingReceiptRecordsIt()
    {
        var order = AwaitingPaymentOrder(out var item);
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
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());

        order.RejectReceipt("Amount does not match the order total");

        Assert.Equal(OrderStatus.AwaitingPayment, order.Status);
        Assert.Null(order.Receipt);
        Assert.Equal("Amount does not match the order total", order.ReceiptRejectionReason);
    }

    [Fact]
    public void GuilderCanResubmitAfterRejection()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());
        order.RejectReceipt("Unreadable screenshot");

        order.SubmitReceipt(PaymentReceipt.Submit("0001111111111", null));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.ReceiptRejectionReason);
    }

    [Fact]
    public void RejectionRequiresAReason()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());

        Assert.Throws<DomainException>(() => order.RejectReceipt(" "));
    }

    [Fact]
    public void CannotRejectAReceiptThatWasNeverSubmitted()
    {
        var order = AwaitingPaymentOrder(out var item);
        Assert.Throws<DomainException>(() => order.RejectReceipt("nope"));
    }

    [Fact]
    public void CannotRejectOnceAcknowledged()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());
        order.Acknowledge(Catalog(item));

        Assert.Throws<DomainException>(() => order.RejectReceipt("too late"));
    }

    // --- Illegal transitions ---------------------------------------------------------

    [Fact]
    public void CannotSkipAcknowledged()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());

        Assert.Throws<DomainException>(order.Release);
    }

    [Fact]
    public void CannotSkipReleased()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());
        order.Acknowledge(Catalog(item));

        Assert.Throws<DomainException>(order.MarkReceived);
    }

    [Fact]
    public void CannotGoBackwards()
    {
        var order = PendingOrder(out var item);
        order.Acknowledge(Catalog(item));
        order.Release();

        Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(item)));
    }

    [Fact]
    public void ReceivedIsTerminal()
    {
        var order = PendingOrder(out var item);
        order.Acknowledge(Catalog(item));
        order.Release();
        order.MarkReceived();

        Assert.Throws<DomainException>(order.Release);
        Assert.Throws<DomainException>(() => order.Cancel("changed my mind"));
    }

    [Fact]
    public void CannotCancelAfterRelease()
    {
        var order = PendingOrder(out var item);
        order.Acknowledge(Catalog(item));
        order.Release();

        Assert.Throws<DomainException>(() => order.Cancel("too late"));
    }

    [Fact]
    public void UnpaidOrderCanBeCancelled()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.Cancel("Never paid");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Never paid", order.CancellationReason);
    }

    [Fact]
    public void CancellingRequiresAReason()
    {
        var order = AwaitingPaymentOrder(out var item);
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

public class MerchSaleTests
{
    [Fact]
    public void SaleComesOffTheVariantPriceNotTheBase()
    {
        var item = HoodieWithPricedSizes();   // S 750, L 780, XL 820
        item.SetSale(true, 20m);

        Assert.Equal(656m, item.PriceFor("XL").Amount);   // 820 - 20%
        Assert.Equal(600m, item.PriceFor("S").Amount);    // 750 - 20%
    }

    [Fact]
    public void ListPriceIsUntouchedSoTheUiCanStrikeItThrough()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);

        Assert.Equal(820m, item.ListPriceFor("XL").Amount);
        Assert.Equal(656m, item.PriceFor("XL").Amount);
    }

    [Fact]
    public void TurningTheSaleOffRestoresTheOriginalPrice()
    {
        var item = HoodieWithPricedSizes();

        item.SetSale(true, 20m);
        Assert.Equal(656m, item.PriceFor("XL").Amount);

        // The percentage is stored, not the discounted amount, so nothing was lost.
        item.SetSale(false, 20m);
        Assert.Equal(820m, item.PriceFor("XL").Amount);
    }

    [Fact]
    public void SaleIsNotAppliedTwice()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);
        item.SetSale(true, 20m);

        // Not 524.80. The discount is computed from the list price every time rather than
        // stored, so setting the same sale again is idempotent.
        Assert.Equal(656m, item.PriceFor("XL").Amount);
    }

    [Fact]
    public void SwitchedOnAtZeroPercentIsNotASale()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 0m);

        Assert.False(item.HasActiveSale);
        Assert.Equal(820m, item.PriceFor("XL").Amount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SalePercentageIsBounded(int percentage)
    {
        var item = HoodieWithPricedSizes();
        Assert.Throws<DomainException>(() => item.SetSale(true, percentage));
    }

    [Fact]
    public void ListingShowsTheCheapestSalePrice()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 10m);

        Assert.Equal(675m, item.PriceFrom.Amount);       // 750 - 10%
        Assert.Equal(750m, item.ListPriceFrom.Amount);
    }

    [Fact]
    public void OrderSnapshotsTheSalePriceNotTheList()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);

        var order = Order.Place(UserId);
        order.AddLine(item, "XL", 1);

        // The guilder is charged the sale price; ending the sale must not reprice history.
        Assert.Equal(656m, order.Lines[0].UnitPrice.Amount);

        item.SetSale(false, 0m);
        Assert.Equal(656m, order.Lines[0].UnitPrice.Amount);
    }
}

public class MerchStockTests
{
    [Fact]
    public void AcknowledgingDeductsStock()
    {
        var item = Hoodie(stock: 5);
        var order = Order.Place(UserId);
        order.AddLine(item, "M", 2);
        order.SubmitReceipt(PaymentReceipt.Submit("0001234567890", null));

        order.Acknowledge(Catalog(item));

        Assert.Equal(3, item.StockFor("M"));

        // Only the variant that sold moves.
        Assert.Equal(5, item.StockFor("S"));
    }

    [Fact]
    public void AcknowledgingIsRefusedWhenStockRanOut()
    {
        var item = Hoodie(stock: 1);
        var order = Order.Place(UserId);
        order.AddLine(item, "M", 1);
        order.SubmitReceipt(PaymentReceipt.Submit("0001234567890", null));

        // Somebody else's order was acknowledged first.
        item.DeductStock("M", 1);

        var ex = Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(item)));
        Assert.Contains("1 ordered, 0 left", ex.Message);

        // Left in Pending so an officer can restock or refund, rather than being handed a
        // half-finished transition.
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void AShortfallOnOneLineTakesNoStockFromTheOthers()
    {
        var hoodie = Hoodie(stock: 5);
        var tote = Tote();
        tote.SetVariantStock(tote.Variants[0].Id, 0);

        var order = Order.Place(UserId);
        order.AddLine(hoodie, "M", 1);
        order.AddLine(tote, "One size", 1);
        order.SubmitReceipt(PaymentReceipt.Submit("0001234567890", null));

        Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(hoodie, tote)));

        // Everything is checked before anything is taken.
        Assert.Equal(5, hoodie.StockFor("M"));
    }

    [Fact]
    public void CartRefusesMoreThanIsLeft()
    {
        var cart = Cart.For(UserId);
        var item = Hoodie(stock: 2);

        var ex = Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 3));
        Assert.Contains("Only 2", ex.Message);
    }

    [Fact]
    public void CartChecksTheRunningTotalAgainstStock()
    {
        var cart = Cart.For(UserId);
        var item = Hoodie(stock: 2);

        cart.AddItem(item, "M", 2);
        Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 1));
    }

    [Fact]
    public void SoldOutSaysSoldOut()
    {
        var cart = Cart.For(UserId);
        var item = Hoodie(stock: 0);

        var ex = Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 1));
        Assert.Contains("sold out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestockingMakesItSellableAgain()
    {
        var item = Hoodie(stock: 0);
        Assert.False(item.CanFulfil("M", 1));

        item.Restock("M", 10);

        Assert.True(item.CanFulfil("M", 1));
        Assert.Equal(10, item.StockFor("M"));
    }

    [Fact]
    public void ItemWithNoVariantsUsesItsOwnStock()
    {
        var item = MerchItem.Create("Sticker pack", "Vinyl", Money.Of(90m));
        AssignId(item, 42);
        item.SetStock(3);

        Assert.Equal(3, item.StockFor(null));
        Assert.True(item.CanFulfil(null, 3));
        Assert.False(item.CanFulfil(null, 4));

        item.DeductStock(null, 3);
        Assert.Equal(0, item.StockFor(null));
    }

    [Fact]
    public void StockCannotGoNegative()
    {
        var item = Hoodie(stock: 1);
        Assert.Throws<DomainException>(() => item.DeductStock("M", 2));
    }
}

public class MerchPreorderTests
{
    private static MerchItem Preorder()
    {
        var item = MerchItem.Create("Cosmic Hoodie", "Bulk print", Money.Of(750m));
        AssignId(item, 7);
        AssignId(item.AddVariant("M", string.Empty, Money.Of(750m)), 1);
        item.SetPreorder(true, null);
        return item;
    }

    [Fact]
    public void PreordersNeverRunOut()
    {
        var item = Preorder();

        // Produced to demand, so "how many are left" has no answer.
        Assert.True(item.CanFulfil("M", 500));
        Assert.Equal(int.MaxValue, item.StockFor("M"));
    }

    [Fact]
    public void AcknowledgingAPreorderDeductsNothing()
    {
        var item = Preorder();
        var order = Order.Place(UserId);
        order.AddLine(item, "M", 3);
        order.SubmitReceipt(PaymentReceipt.Submit("0001234567890", null));

        order.Acknowledge(Catalog(item));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Equal(int.MaxValue, item.StockFor("M"));
    }

    [Fact]
    public void ClosedPreorderStopsAcceptingOrders()
    {
        var item = Preorder();
        item.SetPreorder(true, DateTime.UtcNow.AddMinutes(-1));

        var cart = Cart.For(UserId);
        var ex = Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 1));
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnOpenPreorderWindowStillAccepts()
    {
        var item = Preorder();
        item.SetPreorder(true, DateTime.UtcNow.AddDays(3));

        var cart = Cart.For(UserId);
        cart.AddItem(item, "M", 1);

        Assert.Single(cart.Lines);
    }

    [Fact]
    public void APreorderCannotBeRestocked()
    {
        var ex = Assert.Throws<DomainException>(() => Preorder().Restock("M", 5));
        Assert.Contains("preorder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnlyAPreorderCanHaveAClosingDate()
    {
        var item = Hoodie();
        Assert.Throws<DomainException>(() => item.SetPreorder(false, DateTime.UtcNow.AddDays(1)));
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
