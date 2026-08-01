using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Common;

// Every event names something the guild would recognise as having happened, and carries
// ids rather than aggregates: a handler that needs the object should load it inside its
// own unit of work, not hold a reference to one that has already been saved.

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEvent;

// --- Orders ---------------------------------------------------------------------------
//
// These are the moments an officer or a guilder would describe out loud, which is the
// test for whether something deserves to be an event at all.

public record OrderPlacedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record ReceiptSubmittedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record ReceiptRejectedEvent(Guid OrderId, Guid UserId, string Reason) : DomainEvent;

public record OrderAcknowledgedEvent(Guid OrderId, Guid UserId) : DomainEvent;

/// <summary>
/// Raised instead of <see cref="OrderAcknowledgedEvent"/> when stock ran out: the guilder
/// needs telling that part of what they paid for isn't coming.
/// </summary>
public record OrderAcknowledgedWithShortfallEvent(Guid OrderId, Guid UserId, Money RefundDue) : DomainEvent;

public record OrderReleasedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record OrderReceivedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record OrderCancelledEvent(Guid OrderId, Guid UserId, string Reason) : DomainEvent;

public record RefundSettledEvent(Guid OrderId, Guid UserId, Money Amount, string Reference) : DomainEvent;

/// <summary>A restock let an officer fill a line that was owed a refund.</summary>
public record OrderLineRefulfilledEvent(Guid OrderId, Guid MerchItemId, string? Variant) : DomainEvent;

// --- Merch ----------------------------------------------------------------------------

public record MerchStockDepletedEvent(Guid MerchItemId, string? Variant) : DomainEvent;
