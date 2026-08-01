using MediatR;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// Carries a domain event onto the MediatR bus.
///
/// The wrapper exists so the Domain project never references MediatR: an aggregate
/// raises a plain record, and only this layer knows how it gets delivered. Handlers
/// implement INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;.
/// </summary>
public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

/// <summary>
/// Publishes what the aggregates raised. Implemented in Infrastructure, which calls it
/// after SaveChanges succeeds — a handler must never act on a change that rolled back.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken cancellationToken = default);
}
