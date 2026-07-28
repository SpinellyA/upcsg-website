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

    private Order(int userId)
    {
        if (userId <= 0)
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
    public int UserId { get; private set; }

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

    public Money Total => _lines.Count == 0
        ? Money.Zero()
        : _lines.Aggregate(Money.Zero(), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>Lines are fixed the moment a receipt is submitted.</summary>
    public bool IsEditable => Status == OrderStatus.AwaitingPayment;

    /// <summary>True while the guilder still owes proof of payment.</summary>
    public bool AwaitsPayment => Status == OrderStatus.AwaitingPayment;

    public bool IsOpen => Status is not (OrderStatus.Received or OrderStatus.Cancelled);

    public static Order Place(int userId, string? note = null)
    {
        var order = new Order(userId) { Note = note };
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
            _lines.Add(new OrderLine(item.Id, item.Name, variant, item.Price, quantity));
        }

        Touch();
    }

    public void RemoveLine(int merchItemId, string? variant)
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

        Receipt = null;
        ReceiptRejectionReason = reason.Trim();
    }

    public void Acknowledge() => TransitionTo(OrderStatus.Acknowledged);

    public void Release() => TransitionTo(OrderStatus.Released);

    public void MarkReceived() => TransitionTo(OrderStatus.Received);

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancelling an order requires a reason.");
        }

        TransitionTo(OrderStatus.Cancelled);
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
