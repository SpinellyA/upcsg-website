using MediatR;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Application.Abstractions;

public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<AggregateRoot> aggregates, CancellationToken cancellationToken = default);
}
