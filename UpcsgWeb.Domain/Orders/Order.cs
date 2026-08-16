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

    private Order(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("An order must belong to a user.");
        }

        UserId = userId;

        Status = OrderStatus.AwaitingPayment;
        PlacedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

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

    public static Order Create(Guid userId, string? note = null)
    {
        var order = new Order(userId) { Id = Guid.CreateVersion7(), Note = note };
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

    public void SubmitReceipt(PaymentReceipt receipt)
    {
        if (_lines.Count == 0)
        {
            throw new DomainException("An empty order cannot be paid for.");
        }

        TransitionTo(OrderStatus.Pending);
        Raise(new ReceiptSubmittedEvent(Id, UserId));

        Receipt = receipt;
        ReceiptRejectionReason = null;
    }

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
