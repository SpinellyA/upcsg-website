namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Something that happened in the domain, stated in the past tense.
///
/// Raised by an aggregate as it changes, collected on the root, and dispatched after the
/// unit of work commits. That ordering matters: a handler that emails a guilder must not
/// run for a change that was rolled back.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
