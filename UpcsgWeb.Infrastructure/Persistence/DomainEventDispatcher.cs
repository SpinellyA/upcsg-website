using MediatR;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// Publishes raised events through MediatR, wrapping each one so handlers can subscribe
/// to a concrete event type.
/// </summary>
public class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IEnumerable<AggregateRoot> aggregates,
        CancellationToken cancellationToken = default)
    {
        var roots = aggregates.ToList();

        // Snapshot and clear before publishing. A handler that saves again would
        // otherwise see the same events still sitting on the aggregate and publish
        // them a second time.
        var events = roots.SelectMany(root => root.DomainEvents).ToList();

        foreach (var root in roots)
        {
            root.ClearDomainEvents();
        }

        foreach (var domainEvent in events)
        {
            // Closed over the runtime type so INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
            // is found; publishing the interface would only ever match handlers of IDomainEvent.
            var notification = Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
