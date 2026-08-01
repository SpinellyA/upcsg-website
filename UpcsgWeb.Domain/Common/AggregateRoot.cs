namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Consistency boundary. Only aggregate roots are fetched and saved as a unit, and
/// aggregates reference each other by id — never by navigation property — so one
/// aggregate can never be silently loaded or mutated through another.
///
/// This is also the constraint on IRepository&lt;T&gt;: repositories deal in aggregate
/// roots only, so nothing can fetch a CartLine or OrderLine independently of its parent.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// What this aggregate did during the current unit of work. Read by the save
    /// pipeline after the transaction commits, then cleared.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
