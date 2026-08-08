using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Common;

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEvent;

public record OrderPlacedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record ReceiptSubmittedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record ReceiptRejectedEvent(Guid OrderId, Guid UserId, string Reason) : DomainEvent;

public record OrderAcknowledgedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record OrderAcknowledgedWithShortfallEvent(Guid OrderId, Guid UserId, Money RefundDue) : DomainEvent;

public record OrderReleasedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record OrderReceivedEvent(Guid OrderId, Guid UserId) : DomainEvent;

public record OrderCancelledEvent(Guid OrderId, Guid UserId, string Reason) : DomainEvent;

public record RefundSettledEvent(Guid OrderId, Guid UserId, Money Amount, string Reference) : DomainEvent;

public record OrderLineRefulfilledEvent(Guid OrderId, Guid MerchItemId, string? Variant) : DomainEvent;

public record MerchStockDepletedEvent(Guid MerchItemId, string? Variant) : DomainEvent;
