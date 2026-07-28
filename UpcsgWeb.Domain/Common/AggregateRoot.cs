namespace UpcsgWeb.Domain.Common;

/// <summary>
/// Consistency boundary. Only aggregate roots are fetched and saved as a unit, and
/// aggregates reference each other by id — never by navigation property — so one
/// aggregate can never be silently loaded or mutated through another.
///
/// This is also the constraint on IRepository&lt;T&gt;: repositories deal in aggregate
/// roots only, so nothing can fetch a CartLine or OrderLine independently of its parent.
/// </summary>
public abstract class AggregateRoot : Entity;
