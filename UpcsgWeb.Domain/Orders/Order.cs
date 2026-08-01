using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// A guilder's merch order and the aggregate root over its lines.
///
/// Status is deliberately not settable. Officers move an order forward by calling the
/// intent-named methods, and the transition table below is the only place the rules
/// live — so "Received without ever being Released" is unrepresentable rather than
/// merely discouraged.
/// </summary>
public class Order : AggregateRoot
{
    /// <summary>The only legal moves. Anything absent here is rejected.</summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        // Leaves only by submitting a receipt, or by giving up.
        [OrderStatus.AwaitingPayment] = [OrderStatus.Pending, OrderStatus.Cancelled],

        // An officer can bounce it back if the receipt doesn't check out, rather than
        // having to cancel an order the guilder can still fix.
        [OrderStatus.Pending] = [OrderStatus.Acknowledged, OrderStatus.AwaitingPayment, OrderStatus.Cancelled],

        [OrderStatus.Acknowledged] = [OrderStatus.Released, OrderStatus.Cancelled],

        // Once it's in the guilder's hands, cancelling is a refund conversation,
        // not a status change.
        [OrderStatus.Released] = [OrderStatus.Received],

        [OrderStatus.Received] = [],
        [OrderStatus.Cancelled] = [],
    };

    private readonly List<OrderLine> _lines = [];

    private Order() { } // EF

    private Order(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("An order must belong to a user.");
        }

        UserId = userId;

        // Checkout does not mean paid. The order waits here until a receipt arrives.
        Status = OrderStatus.AwaitingPayment;
        PlacedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reference to the AppUser aggregate by id only.</summary>
    public Guid UserId { get; private set; }

    public OrderStatus Status { get; private set; }
    public DateTime PlacedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Free-text note from the guilder (preferred pickup time, etc.).</summary>
    public string? Note { get; private set; }

    /// <summary>Set when an officer cancels, so the reason survives.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>GCash proof, once the guilder submits it. Null while AwaitingPayment.</summary>
    public PaymentReceipt? Receipt { get; private set; }

    /// <summary>Why an officer sent the receipt back, if they did.</summary>
    public string? ReceiptRejectionReason { get; private set; }

    /// <summary>Read-only to callers: lines are only added through AddLine.</summary>
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// What the order came to. Note this does NOT shrink when a line falls short — the
    /// guilder paid this, and quietly reducing it would erase the evidence that money is
    /// owed back.
    /// </summary>
    public Money Total => _lines.Count == 0
        ? Money.Zero()
        : _lines.Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>
    /// What the guilder actually handed over, captured when an officer confirmed the
    /// receipt. Kept apart from Total so "paid ₱2,460, delivering ₱1,640, owe ₱820" is a
    /// fact the system holds rather than something an officer works out on paper.
    /// </summary>
    public Money? AmountPaid { get; private set; }

    /// <summary>Money owed back for lines that couldn't be filled and haven't been settled.</summary>
    public Money RefundDue => _lines
        .Where(l => l.IsRefundDue)
        .Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    public bool HasRefundDue => _lines.Any(l => l.IsRefundDue);

    /// <summary>True once every shortfall on this order has been paid back.</summary>
    public bool RefundSettled => RefundReference is not null;

    /// <summary>GCash reference for the money sent back. Null until it has been.</summary>
    public string? RefundReference { get; private set; }

    public DateTime? RefundSettledAt { get; private set; }

    /// <summary>What the guilder is actually receiving, after shortfalls.</summary>
    public Money FulfilledTotal => _lines
        .Where(l => l.Status == OrderLineStatus.ToFulfil)
        .Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>Lines are fixed the moment a receipt is submitted.</summary>
    public bool IsEditable => Status == OrderStatus.AwaitingPayment;

    /// <summary>True while the guilder still owes proof of payment.</summary>
    public bool AwaitsPayment => Status == OrderStatus.AwaitingPayment;

    public bool IsOpen => Status is not (OrderStatus.Received or OrderStatus.Cancelled);

    public static Order Create(Guid userId, string? note = null)
    {
        var order = new Order(userId) { Id = Guid.CreateVersion7(), Note = note };
        order.Raise(new OrderPlacedEvent(order.Id, userId));
        return order;
    }

    /// <summary>
    /// Adds an item, copying the name and price as they stand right now.
    /// Takes the MerchItem so the snapshot can't be faked by the caller — a client
    /// supplying its own price would be free to order a hoodie for one peso.
    /// </summary>
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

        // Same item and variant twice is a quantity change, not a second line.
        var existing = _lines.FirstOrDefault(l => l.MerchItemId == item.Id && l.Variant == variant);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
        }
        else
        {
            // PriceFor, not item.Price: variants carry their own price now, and snapshotting
            // the base price here would sell the dearest size at the cheapest one's cost.
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

    // --- Lifecycle ------------------------------------------------------------------

    /// <summary>
    /// Guilder submits GCash proof. This is what moves the order into the officers'
    /// queue — the only route out of AwaitingPayment other than cancelling.
    /// </summary>
    public void SubmitReceipt(PaymentReceipt receipt)
    {
        if (_lines.Count == 0)
        {
            throw new DomainException("An empty order cannot be paid for.");
        }

        // TransitionTo rejects this from any state but AwaitingPayment, so a receipt
        // can't be swapped out after an officer has already acted on it.
        TransitionTo(OrderStatus.Pending);
        Raise(new ReceiptSubmittedEvent(Id, UserId));

        Receipt = receipt;
        ReceiptRejectionReason = null;
    }

    /// <summary>
    /// Officer sends a receipt back — wrong amount, unreadable screenshot, no such
    /// reference. Returns the order to the guilder rather than killing it outright.
    /// </summary>
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

    /// <summary>
    /// Officer confirms the payment landed. This is the moment stock actually moves: the
    /// first point at which the money is known to be real, and late enough that an
    /// abandoned unpaid cart never held a unit hostage.
    ///
    /// The caller supplies the items so the deduction happens against the live records
    /// rather than the order's own snapshots — a snapshot cannot tell you what is left.
    /// Throws if anything on the order can no longer be filled, leaving the order in
    /// Pending so an officer can restock or refund rather than being handed a half-done
    /// transition.
    /// </summary>
    public void Acknowledge(IReadOnlyDictionary<Guid, MerchItem> items)
    {
        // Check everything before taking anything, so a shortfall on the second line
        // cannot leave the first line's stock already deducted.
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
                    $"Not enough {line.ItemName}{(line.Variant is null ? "" : $" ({line.Variant})")} — "
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

    /// <summary>
    /// Acknowledges anyway, taking what stock exists and marking the rest as owed back.
    ///
    /// Deliberately a separate method rather than a flag on Acknowledge: creating a refund
    /// obligation is a decision an officer makes knowingly, not a fallback that happens
    /// because a boolean was left at its default.
    ///
    /// Returns the lines that fell short, so the caller can tell the guilder precisely
    /// what is being refunded.
    /// </summary>
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

    /// <summary>
    /// A restock arrived, so a line that fell short can be filled after all. The officer
    /// chooses this — a restock never silently resurrects an order, because the guilder
    /// may already have been told they're being refunded.
    /// </summary>
    public void RefulfilLine(Guid merchItemId, string? variant, IReadOnlyDictionary<Guid, MerchItem> items)
    {
        // Checked before the line lookup: once settled, every line is Refunded rather than
        // RefundDue, so the lookup would fail first and report "not awaiting a refund" —
        // true, but useless. The officer needs to know the money already went back.
        if (RefundSettled)
        {
            throw new DomainException(
                "This order's refund has already been sent. You cannot un-send GCash — "
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
            throw new DomainException($"Still not enough — {line.Quantity} needed, {left} left.");
        }

        item.DeductStock(variant, line.Quantity);
        line.RestoreToFulfil();
        Touch();
    }

    /// <summary>
    /// Records that the money went back, with the GCash reference. Terminal for those
    /// lines: this is the point after which a restock can no longer rescue them.
    /// </summary>
    public void SettleRefund(string reference)
    {
        if (!HasRefundDue)
        {
            throw new DomainException("This order has nothing owing.");
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(
                "Recording a refund requires the GCash reference — without it the Auditor "
                + "cannot verify the money actually moved.");
        }

        // Captured before the loop: MarkRefunded clears IsRefundDue, so reading
        // RefundDue afterwards would report zero back to whoever is listening.
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

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancelling an order requires a reason.");
        }

        TransitionTo(OrderStatus.Cancelled);
        Raise(new OrderCancelledEvent(Id, UserId, reason.Trim()));
        CancellationReason = reason;
    }

    private void TransitionTo(OrderStatus next)
    {
        // An empty order must never reach a stage that implies goods exist.
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
