using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Orders;

public class Order : AggregateRoot
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.AwaitingPayment] = [OrderStatus.Pending, OrderStatus.Cancelled],

        [OrderStatus.Pending] = [OrderStatus.Acknowledged, OrderStatus.AwaitingPayment, OrderStatus.Cancelled],

        [OrderStatus.Acknowledged] = [OrderStatus.Released, OrderStatus.Cancelled],

        [OrderStatus.Released] = [OrderStatus.Received],

        [OrderStatus.Received] = [],
        [OrderStatus.Cancelled] = [],
    };

    private readonly List<OrderLine> _lines = [];

    private Order() { }

    private Order(Guid userId, PaymentMethod paymentMethod)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("An order must belong to a user.");
        }

        UserId = userId;
        PaymentMethod = paymentMethod;

        // Every order starts editable so its lines can be added, whatever it will be paid
        // with. A cash order is moved into the officers' queue by QueueForCashPayment once
        // it is complete.
        Status = OrderStatus.AwaitingPayment;

        PlacedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public bool IsCash => PaymentMethod == PaymentMethod.Cash;

    public OrderStatus Status { get; private set; }
    public DateTime PlacedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string? Note { get; private set; }

    public string? CancellationReason { get; private set; }

    public PaymentReceipt? Receipt { get; private set; }

    public string? ReceiptRejectionReason { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total => _lines.Count == 0
        ? Money.Zero()
        : _lines.Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    public Money? AmountPaid { get; private set; }

    public Money RefundDue => _lines
        .Where(l => l.IsRefundDue)
        .Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    public bool HasRefundDue => _lines.Any(l => l.IsRefundDue);

    public bool RefundSettled => RefundReference is not null;

    public string? RefundReference { get; private set; }

    public DateTime? RefundSettledAt { get; private set; }

    public Money FulfilledTotal => _lines
        .Where(l => l.Status == OrderLineStatus.ToFulfil)
        .Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    public bool IsEditable => Status == OrderStatus.AwaitingPayment;

    public bool AwaitsPayment => Status == OrderStatus.AwaitingPayment;

    public bool IsOpen => Status is not (OrderStatus.Received or OrderStatus.Cancelled);

    public static Order Create(
        Guid userId,
        PaymentMethod paymentMethod = PaymentMethod.GCash,
        string? note = null)
    {
        var order = new Order(userId, paymentMethod) { Id = Guid.CreateVersion7(), Note = note };
        order.Raise(new OrderPlacedEvent(order.Id, userId));
        return order;
    }

    public void AddLine(MerchItem item, string? variant, int quantity)
    {
        EnsureEditable();

        if (!item.InStock)
        {
            throw new DomainException($"{item.Name} is out of stock.");
        }

        if (variant is not null && !item.HasVariant(variant))
        {
            throw new DomainException($"{item.Name} has no variant '{variant}'.");
        }

        var existing = _lines.FirstOrDefault(l => l.MerchItemId == item.Id && l.Variant == variant);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
        }
        else
        {
            _lines.Add(new OrderLine(item.Id, item.Name, variant, item.PriceFor(variant), quantity));
        }

        Touch();
    }

    public void RemoveLine(Guid merchItemId, string? variant)
    {
        EnsureEditable();

        var line = _lines.FirstOrDefault(l => l.MerchItemId == merchItemId && l.Variant == variant)
            ?? throw new DomainException("That item is not on this order.");

        _lines.Remove(line);
        Touch();
    }

    /// <summary>
    /// Hands a finished cash order to the officers. There is nothing for the guilder to
    /// submit, so this is the cash counterpart of sending a payment reference: it closes the
    /// order to further edits and puts it in the queue to be paid in person and recorded.
    /// No stock is committed here, which is what leaves a cash order behind an online one
    /// when both want the last of something.
    /// </summary>
    public void QueueForCashPayment()
    {
        if (!IsCash)
        {
            throw new DomainException("Only a cash order waits to be paid in person.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("An empty order cannot be paid for.");
        }

        TransitionTo(OrderStatus.Pending);
    }

    public void SubmitReceipt(PaymentReceipt receipt)
    {
        if (_lines.Count == 0)
        {
            throw new DomainException("An empty order cannot be paid for.");
        }

        if (IsCash)
        {
            throw new DomainException(
                "This is a cash order. Pay an officer in person and they will record it; "
                + "there is no reference to submit.");
        }

        TransitionTo(OrderStatus.Pending);
        Raise(new ReceiptSubmittedEvent(Id, UserId));

        Receipt = receipt;
        ReceiptRejectionReason = null;
    }

    /// <summary>
    /// Confirms an online payment without anyone checking it, because there is no payment
    /// provider wired up to check it against. Stock comes off straight away, and officers
    /// cancel the ones that turn out to be bad, which puts it back.
    /// <para>
    /// Kept apart from <see cref="SubmitReceipt"/> deliberately. This is a stopgap for having
    /// no payment API, not a rule about what an order is: when one is wired up, the caller
    /// stops invoking this and the order waits for a real verification instead, with nothing
    /// else in the aggregate needing to change.
    /// </para>
    /// </summary>
    /// <returns>
    /// Lines that could not be filled and are owed a refund. Empty when everything was filled,
    /// and also when nothing could be, in which case the order is left for an officer.
    /// </returns>
    public IReadOnlyList<OrderLine> ConfirmOnlinePaymentUnchecked(
        IReadOnlyDictionary<Guid, MerchItem> items)
    {
        if (IsCash)
        {
            throw new DomainException("A cash order is confirmed by an officer, not automatically.");
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Only an order awaiting confirmation can be confirmed.");
        }

        // The money has already moved, so a shortfall must not throw the reference away. Where
        // nothing at all can be filled, the order stays in the queue for an officer to cancel
        // and refund in full: AcknowledgeWithShortfall would refuse it outright.
        if (!CanFillAnything(items))
        {
            return [];
        }

        return AcknowledgeWithShortfall(items);
    }

    /// <summary>Whether at least one line can still be filled from the given stock.</summary>
    public bool CanFillAnything(IReadOnlyDictionary<Guid, MerchItem> items) =>
        _lines.Any(l => items.TryGetValue(l.MerchItemId, out var item)
                     && item.CanFulfil(l.Variant, l.Quantity));

    public void RejectReceipt(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Rejecting a receipt requires a reason.");
        }

        TransitionTo(OrderStatus.AwaitingPayment);
        Raise(new ReceiptRejectedEvent(Id, UserId, reason.Trim()));

        Receipt = null;
        ReceiptRejectionReason = reason.Trim();
    }

    public void Acknowledge(IReadOnlyDictionary<Guid, MerchItem> items)
    {
        foreach (var line in _lines)
        {
            if (!items.TryGetValue(line.MerchItemId, out var item))
            {
                throw new DomainException($"{line.ItemName} no longer exists, so this order cannot be filled.");
            }

            if (!item.CanFulfil(line.Variant, line.Quantity))
            {
                var left = item.StockFor(line.Variant);
                throw new DomainException(
                    $"Not enough {line.ItemName}{(line.Variant is null ? "" : $" ({line.Variant})")}: "
                    + $"{line.Quantity} ordered, {left} left.");
            }
        }

        foreach (var line in _lines)
        {
            items[line.MerchItemId].DeductStock(line.Variant, line.Quantity);
        }

        AmountPaid = Total;
        TransitionTo(OrderStatus.Acknowledged);
        Raise(new OrderAcknowledgedEvent(Id, UserId));
    }

    public IReadOnlyList<OrderLine> AcknowledgeWithShortfall(IReadOnlyDictionary<Guid, MerchItem> items)
    {
        var short_ = new List<OrderLine>();

        foreach (var line in _lines)
        {
            if (!items.TryGetValue(line.MerchItemId, out var item))
            {
                line.MarkRefundDue("This item is no longer in the store.");
                short_.Add(line);
                continue;
            }

            if (item.CanFulfil(line.Variant, line.Quantity))
            {
                item.DeductStock(line.Variant, line.Quantity);
                continue;
            }

            var left = item.StockFor(line.Variant);
            line.MarkRefundDue(left == 0
                ? "Sold out before we could fill this."
                : $"Only {left} left, {line.Quantity} were ordered.");

            short_.Add(line);
        }

        if (short_.Count == _lines.Count)
        {
            throw new DomainException(
                "Nothing on this order can be filled. Cancel and refund it in full instead.");
        }

        AmountPaid = Total;
        TransitionTo(OrderStatus.Acknowledged);
        Raise(new OrderAcknowledgedWithShortfallEvent(Id, UserId, RefundDue));

        return short_;
    }

    public void RefulfilLine(Guid merchItemId, string? variant, IReadOnlyDictionary<Guid, MerchItem> items)
    {
        if (RefundSettled)
        {
            throw new DomainException(
                "This order's refund has already been sent. You cannot un-send GCash, so "
                + "ask the guilder to place a new order.");
        }

        var line = _lines.FirstOrDefault(l =>
            l.MerchItemId == merchItemId && l.Variant == variant && l.IsRefundDue)
            ?? throw new DomainException("That line is not awaiting a refund.");

        if (!items.TryGetValue(merchItemId, out var item))
        {
            throw new DomainException($"{line.ItemName} is no longer in the store.");
        }

        if (!item.CanFulfil(variant, line.Quantity))
        {
            var left = item.StockFor(variant);
            throw new DomainException($"Still not enough: {line.Quantity} needed, {left} left.");
        }

        item.DeductStock(variant, line.Quantity);
        line.RestoreToFulfil();
        Touch();
    }

    public void SettleRefund(string reference)
    {
        if (!HasRefundDue)
        {
            throw new DomainException("This order has nothing owing.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(
                "Recording a refund requires the GCash reference. Without it the Auditor "
                + "cannot verify the money actually moved.");
        }

        var owed = RefundDue;

        foreach (var line in _lines.Where(l => l.IsRefundDue).ToList())
        {
            line.MarkRefunded();
        }

        RefundReference = reference.Trim();
        RefundSettledAt = DateTime.UtcNow;
        Raise(new RefundSettledEvent(Id, UserId, owed, RefundReference));
        Touch();
    }

    public void Release()
    {
        TransitionTo(OrderStatus.Released);
        Raise(new OrderReleasedEvent(Id, UserId));
    }

    public void MarkReceived()
    {
        TransitionTo(OrderStatus.Received);
        Raise(new OrderReceivedEvent(Id, UserId));
    }

    // Cancelling an acknowledged order has to put the stock back. Acknowledging is what
    // deducts it, so before this the shirts stayed "sold" forever and the store showed
    // fewer than were actually on the shelf - with no way to notice except counting.
    //
    // Only lines that were actually deducted come back: a line marked refund-due was
    // never taken from stock, and a refunded one has been settled in money instead.
    // Cancelling before acknowledgement touches nothing, because nothing was taken.
    public IReadOnlyList<OrderLine> Cancel(
        string reason,
        IReadOnlyDictionary<Guid, MerchItem> items)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancelling an order requires a reason.");
        }

        var returned = new List<OrderLine>();

        if (Status == OrderStatus.Acknowledged)
        {
            foreach (var line in _lines.Where(l => l.Status == OrderLineStatus.ToFulfil))
            {
                // An item deleted from the catalogue since checkout has nowhere to go
                // back to. That is not a reason to block the cancellation.
                if (!items.TryGetValue(line.MerchItemId, out var item))
                {
                    continue;
                }

                item.Restock(line.Variant, line.Quantity);
                returned.Add(line);
            }
        }

        TransitionTo(OrderStatus.Cancelled);
        Raise(new OrderCancelledEvent(Id, UserId, reason.Trim()));
        CancellationReason = reason;

        return returned;
    }

    private void TransitionTo(OrderStatus next)
    {
        if (next is OrderStatus.Pending or OrderStatus.Acknowledged && _lines.Count == 0)
        {
            throw new DomainException("An empty order cannot be progressed.");
        }

        var allowed = AllowedTransitions[Status];
        if (!allowed.Contains(next))
        {
            throw new DomainException(
                allowed.Length == 0
                    ? $"Order is already {Status} and cannot change."
                    : $"Cannot move an order from {Status} to {next}. Allowed: {string.Join(", ", allowed)}.");
        }

        Status = next;
        Touch();
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException($"An order that is {Status} can no longer be edited.");
        }
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
