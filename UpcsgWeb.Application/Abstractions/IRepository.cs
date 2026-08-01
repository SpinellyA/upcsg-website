using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// The CRUD every aggregate needs. Specific repositories inherit this and add only the
/// queries their aggregate actually has — so an interface with nothing in its body is
/// the expected case, not an omission.
///
/// Constrained to <see cref="AggregateRoot"/> on purpose: repositories deal in
/// consistency boundaries. Without that constraint you could fetch an OrderLine or
/// CartLine on its own and mutate it outside the aggregate that enforces its rules.
///
/// Deliberately no IQueryable. Exposing one would let callers compose arbitrary SQL
/// through the abstraction, which leaks persistence into the caller and makes the seam
/// worthless. Anything more specific than these methods earns a named method instead.
/// </summary>
public interface IRepository<T> where T : AggregateRoot
{
    /// <summary>Tracked, so the result can be mutated and saved.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Read-only; not tracked.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Stages an insert. Synchronous because nothing reaches the database until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> runs.
    /// </summary>
    void Add(T entity);

    void Remove(T entity);
}
