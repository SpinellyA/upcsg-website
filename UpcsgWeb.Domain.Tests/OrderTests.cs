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
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 1);
        return order;
    }

    private static PaymentReceipt Receipt() => PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890");

    [Fact]
    public void CheckoutDoesNotMeanPaid_OrderStartsAwaitingPayment()
    {
        var order = Order.Create(UserId);
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

    [Fact]
    public void OfficerCannotAcknowledgeBeforeAReceiptArrives()
    {
        var order = AwaitingPaymentOrder(out var item);

        var ex = Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(item)));
        Assert.Contains("AwaitingPayment", ex.Message);
    }

    [Fact]
    public void CannotSubmitReceiptTwice()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(Receipt());

        Assert.Throws<DomainException>(() => order.SubmitReceipt(Receipt()));
    }

    [Fact]
    public void SubmittingReceiptRecordsIt()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/x.png", "  0009876543210  "));

        Assert.NotNull(order.Receipt);
        Assert.Equal("0009876543210", order.Receipt!.ReferenceNumber);
        Assert.Equal("https://cdn/x.png", order.Receipt.ScreenshotUrl);
    }

    [Fact]
    public void ReceiptRequiresAScreenshot() =>
        Assert.Throws<DomainException>(() => PaymentReceipt.FromScreenshot("   ", "0001234567890"));

    [Fact]
    public void ReceiptDoesNotRequireAReferenceNumber()
    {
        var receipt = PaymentReceipt.FromScreenshot("https://cdn/receipt.png");

        Assert.Null(receipt.ReferenceNumber);
        Assert.Equal("https://cdn/receipt.png", receipt.ScreenshotUrl);
    }

    [Fact]
    public void ABlankReferenceIsStoredAsNoReferenceAtAll()
    {
        var receipt = PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "   ");

        Assert.Null(receipt.ReferenceNumber);
    }

    [Fact]
    public void AnOverlongReferenceIsStillRefused() =>
        Assert.Throws<DomainException>(() =>
            PaymentReceipt.FromScreenshot("https://cdn/receipt.png", new string('9', 51)));

    [Fact]
    public void EmptyOrderCannotBePaidFor()
    {
        var order = Order.Create(UserId);
        Assert.Throws<DomainException>(() => order.SubmitReceipt(Receipt()));
    }

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

        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001111111111"));

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
        Assert.Throws<DomainException>(() => order.Cancel("changed my mind", Catalog(item)));
    }

    [Fact]
    public void CannotCancelAfterRelease()
    {
        var order = PendingOrder(out var item);
        order.Acknowledge(Catalog(item));
        order.Release();

        Assert.Throws<DomainException>(() => order.Cancel("too late", Catalog(item)));
    }

    [Fact]
    public void UnpaidOrderCanBeCancelled()
    {
        var order = AwaitingPaymentOrder(out var item);
        order.Cancel("Never paid", Catalog(item));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Never paid", order.CancellationReason);
    }

    [Fact]
    public void CancellingRequiresAReason()
    {
        var order = AwaitingPaymentOrder(out var item);
        Assert.Throws<DomainException>(() => order.Cancel("  ", Catalog(item)));
    }

    [Fact]
    public void CancellingAnAcknowledgedOrderPutsTheStockBack()
    {
        var order = AwaitingPaymentOrder(out var item);
        var before = item.StockFor("M");

        order.SubmitReceipt(Receipt());
        order.Acknowledge(Catalog(item));

        Assert.Equal(before - 1, item.StockFor("M"));

        var returned = order.Cancel("Guilder backed out", Catalog(item));

        Assert.Single(returned);
        Assert.Equal(before, item.StockFor("M"));
    }

    [Fact]
    public void CancellingBeforeAcknowledgementReturnsNothing()
    {
        var order = AwaitingPaymentOrder(out var item);
        var before = item.StockFor("M");

        // Nothing was deducted, so nothing may come back - otherwise cancelling an
        // unpaid order would invent stock out of nowhere.
        var returned = order.Cancel("Never paid", Catalog(item));

        Assert.Empty(returned);
        Assert.Equal(before, item.StockFor("M"));
    }

    [Fact]
    public void CancellingReturnsOnlyTheLinesThatWereActuallyFilled()
    {
        var hoodie = Hoodie();
        hoodie.SetVariantStock(hoodie.Variants.First(v => v.Name == "M").Id, 1);

        var tote = Tote();
        var toteBefore = tote.StockFor("One size");

        var order = Order.Create(UserId);
        order.AddLine(hoodie, "M", 3);   // only one in stock, so this goes refund-due
        order.AddLine(tote, "One size", 2);
        order.SubmitReceipt(Receipt());

        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        // The hoodie line never moved stock; the tote line did.
        Assert.Equal(1, hoodie.StockFor("M"));
        Assert.Equal(toteBefore - 2, tote.StockFor("One size"));

        var returned = order.Cancel("Cannot supply", Catalog(hoodie, tote));

        Assert.Single(returned);
        Assert.Equal(1, hoodie.StockFor("M"));            // unchanged, nothing to give back
        Assert.Equal(toteBefore, tote.StockFor("One size"));
    }

    [Fact]
    public void LinesFreezeOnceAReceiptIsSubmitted()
    {
        var order = AwaitingPaymentOrder(out var item);
        Assert.True(order.IsEditable);

        order.SubmitReceipt(Receipt());

        Assert.False(order.IsEditable);
        Assert.Throws<DomainException>(() => order.AddLine(item, "L", 1));
    }

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
        var order = Order.Create(UserId);
        order.AddLine(Hoodie(), "M", 2);
        order.AddLine(Tote(), "One size", 1);

        Assert.Equal(1750m, order.Total.Amount);
    }

    [Fact]
    public void LineTakesTheVariantsPriceNotTheItemBase()
    {
        var order = Order.Create(UserId);
        order.AddLine(HoodieWithPricedSizes(), "XL", 1);

        Assert.Equal(820m, order.Lines[0].UnitPrice.Amount);
    }

    [Fact]
    public void EachVariantIsPricedIndependentlyOnTheSameOrder()
    {
        var item = HoodieWithPricedSizes();
        var order = Order.Create(UserId);

        order.AddLine(item, "S", 1);
        order.AddLine(item, "XL", 1);

        Assert.Equal(1570m, order.Total.Amount);
    }

    [Fact]
    public void ItemWithNoVariantsFallsBackToItsBasePrice()
    {
        var item = MerchItem.Create("Sticker pack", "Vinyl", Money.Of(90m));

        var order = Order.Create(UserId);
        order.AddLine(item, null, 2);

        Assert.Equal(180m, order.Total.Amount);
    }
}

public class RefundDueTests
{
    private static Order ShortOrder(out MerchItem hoodie, out MerchItem tote)
    {
        hoodie = Hoodie(stock: 1, price: 750m);
        tote = Tote(price: 250m);

        var order = Order.Create(UserId);
        order.AddLine(hoodie, "M", 2);
        order.AddLine(tote, "One size", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        return order;
    }

    [Fact]
    public void PartialAcknowledgeFillsWhatItCanAndOwesTheRest()
    {
        var order = ShortOrder(out var hoodie, out var tote);

        var shortfall = order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Single(shortfall);
        Assert.Equal("Cosmic Hoodie", shortfall[0].ItemName);

        Assert.Equal(99, tote.StockFor("One size"));
        Assert.Equal(1, hoodie.StockFor("M"));
    }

    [Fact]
    public void TotalStaysWhatWasPaidAndTheShortfallIsRecordedSeparately()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        Assert.Equal(1750m, order.Total.Amount);
        Assert.Equal(1750m, order.AmountPaid!.Amount);
        Assert.Equal(1500m, order.RefundDue.Amount);
        Assert.Equal(250m, order.FulfilledTotal.Amount);
    }

    [Fact]
    public void TheShortfallReasonSaysWhatActuallyHappened()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        var shortfall = order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        Assert.Equal("Only 1 left, 2 were ordered.", shortfall[0].ShortfallReason);
    }

    [Fact]
    public void AnOrderWhereNothingCanBeFilledIsRefusedOutright()
    {
        var hoodie = Hoodie(stock: 0);
        var order = Order.Create(UserId);
        order.AddLine(hoodie, "M", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        var ex = Assert.Throws<DomainException>(() => order.AcknowledgeWithShortfall(Catalog(hoodie)));
        Assert.Contains("Cancel and refund it in full", ex.Message);
    }

    [Fact]
    public void RestockingLetsAnOfficerFillTheLineAfterAll()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));
        Assert.True(order.HasRefundDue);

        hoodie.Restock("M", 10);
        order.RefulfilLine(hoodie.Id, "M", Catalog(hoodie, tote));

        Assert.False(order.HasRefundDue);
        Assert.Equal(0m, order.RefundDue.Amount);
        Assert.Equal(1750m, order.FulfilledTotal.Amount);

        Assert.Equal(9, hoodie.StockFor("M"));
    }

    [Fact]
    public void RefulfillingNeedsEnoughStockForTheWholeLine()
    {
        var hoodie = Hoodie(stock: 1);
        var tote = Tote();

        var order = Order.Create(UserId);
        order.AddLine(hoodie, "M", 3);
        order.AddLine(tote, "One size", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        hoodie.Restock("M", 1);

        var ex = Assert.Throws<DomainException>(
            () => order.RefulfilLine(hoodie.Id, "M", Catalog(hoodie, tote)));

        Assert.Contains("3 needed, 2 left", ex.Message);
        Assert.True(order.HasRefundDue);
    }

    [Fact]
    public void OnceTheMoneyHasGoneBackARestockCannotUndoIt()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));
        order.SettleRefund("0009999999999");

        hoodie.Restock("M", 10);

        var ex = Assert.Throws<DomainException>(
            () => order.RefulfilLine(hoodie.Id, "M", Catalog(hoodie, tote)));

        Assert.Contains("already been sent", ex.Message);
    }

    [Fact]
    public void SettlingRecordsTheReferenceAndClosesTheObligation()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        order.SettleRefund("  0009999999999  ");

        Assert.True(order.RefundSettled);
        Assert.Equal("0009999999999", order.RefundReference);
        Assert.NotNull(order.RefundSettledAt);

        Assert.False(order.HasRefundDue);
        Assert.Equal(OrderLineStatus.Refunded, order.Lines[0].Status);
    }

    [Fact]
    public void SettlingRequiresAReference()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        var ex = Assert.Throws<DomainException>(() => order.SettleRefund("   "));
        Assert.Contains("GCash reference", ex.Message);
    }

    [Fact]
    public void CannotSettleAnOrderThatOwesNothing()
    {
        var order = PendingOrder(out var item);
        order.Acknowledge(Catalog(item));

        Assert.Throws<DomainException>(() => order.SettleRefund("0009999999999"));
    }

    [Fact]
    public void AShortOrderStillMovesThroughReleaseAndReceipt()
    {
        var order = ShortOrder(out var hoodie, out var tote);
        order.AcknowledgeWithShortfall(Catalog(hoodie, tote));

        order.Release();
        order.MarkReceived();

        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.True(order.HasRefundDue);
    }

    [Fact]
    public void StrictAcknowledgeStillRefusesRatherThanCreatingARefund()
    {
        var order = ShortOrder(out var hoodie, out var tote);

        Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(hoodie, tote)));
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.False(order.HasRefundDue);
    }
}

public class MerchSaleTests
{
    [Fact]
    public void SaleComesOffTheVariantPriceNotTheBase()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);

        Assert.Equal(656m, item.PriceFor("XL").Amount);
        Assert.Equal(600m, item.PriceFor("S").Amount);
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

        item.SetSale(false, 20m);
        Assert.Equal(820m, item.PriceFor("XL").Amount);
    }

    [Fact]
    public void SaleIsNotAppliedTwice()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);
        item.SetSale(true, 20m);

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

        Assert.Equal(675m, item.PriceFrom.Amount);
        Assert.Equal(750m, item.ListPriceFrom.Amount);
    }

    [Fact]
    public void OrderSnapshotsTheSalePriceNotTheList()
    {
        var item = HoodieWithPricedSizes();
        item.SetSale(true, 20m);

        var order = Order.Create(UserId);
        order.AddLine(item, "XL", 1);

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
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 2);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        order.Acknowledge(Catalog(item));

        Assert.Equal(3, item.StockFor("M"));

        Assert.Equal(5, item.StockFor("S"));
    }

    [Fact]
    public void AcknowledgingIsRefusedWhenStockRanOut()
    {
        var item = Hoodie(stock: 1);
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        item.DeductStock("M", 1);

        var ex = Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(item)));
        Assert.Contains("1 ordered, 0 left", ex.Message);

        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void AShortfallOnOneLineTakesNoStockFromTheOthers()
    {
        var hoodie = Hoodie(stock: 5);
        var tote = Tote();
        tote.SetVariantStock(tote.Variants[0].Id, 0);

        var order = Order.Create(UserId);
        order.AddLine(hoodie, "M", 1);
        order.AddLine(tote, "One size", 1);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        Assert.Throws<DomainException>(() => order.Acknowledge(Catalog(hoodie, tote)));

        Assert.Equal(5, hoodie.StockFor("M"));
    }

    [Fact]
    public void CartRefusesMoreThanIsLeft()
    {
        var cart = Cart.Create(UserId);
        var item = Hoodie(stock: 2);

        var ex = Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 3));
        Assert.Contains("Only 2", ex.Message);
    }

    [Fact]
    public void CartChecksTheRunningTotalAgainstStock()
    {
        var cart = Cart.Create(UserId);
        var item = Hoodie(stock: 2);

        cart.AddItem(item, "M", 2);
        Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 1));
    }

    [Fact]
    public void SoldOutSaysSoldOut()
    {
        var cart = Cart.Create(UserId);
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
        item.AddVariant("M", string.Empty, Money.Of(750m));
        item.SetPreorder(true, null);
        return item;
    }

    [Fact]
    public void PreordersNeverRunOut()
    {
        var item = Preorder();

        Assert.True(item.CanFulfil("M", 500));
        Assert.Equal(int.MaxValue, item.StockFor("M"));
    }

    [Fact]
    public void AcknowledgingAPreorderDeductsNothing()
    {
        var item = Preorder();
        var order = Order.Create(UserId);
        order.AddLine(item, "M", 3);
        order.SubmitReceipt(PaymentReceipt.FromScreenshot("https://cdn/receipt.png", "0001234567890"));

        order.Acknowledge(Catalog(item));

        Assert.Equal(OrderStatus.Acknowledged, order.Status);
        Assert.Equal(int.MaxValue, item.StockFor("M"));
    }

    [Fact]
    public void ClosedPreorderStopsAcceptingOrders()
    {
        var item = Preorder();
        item.SetPreorder(true, DateTime.UtcNow.AddMinutes(-1));

        var cart = Cart.Create(UserId);
        var ex = Assert.Throws<DomainException>(() => cart.AddItem(item, "M", 1));
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnOpenPreorderWindowStillAccepts()
    {
        var item = Preorder();
        item.SetPreorder(true, DateTime.UtcNow.AddDays(3));

        var cart = Cart.Create(UserId);
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

        Assert.False(Hoodie().HasPriceRange);
    }

    [Fact]
    public void DuplicateVariantNamesAreRejected()
    {
        var item = MerchItem.Create("Tee", "Cotton", Money.Of(450m));
        item.AddVariant("M", string.Empty, Money.Of(450m));

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
