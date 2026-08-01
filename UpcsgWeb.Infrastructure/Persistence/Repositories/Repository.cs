using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the shared CRUD. Concrete repositories inherit this and
/// add only what their aggregate genuinely needs.
///
/// The important extension point is <see cref="Query"/>. An aggregate that spans more
/// than one table (Order with its lines, Cart with its lines) overrides it once to add
/// the Include, and every inherited method — GetByIdAsync, GetAllAsync, and any derived
/// query — then loads the aggregate whole. Without that, the base GetByIdAsync would
/// hand back an Order with an empty Lines collection and a total of zero.
/// </summary>
public abstract class Repository<T>(UpcsgDbContext db) : IRepository<T>
    where T : AggregateRoot
{
    protected UpcsgDbContext Db { get; } = db;

    protected DbSet<T> Set => Db.Set<T>();

    /// <summary>Tracked query. Override to Include whatever completes the aggregate.</summary>
    protected virtual IQueryable<T> Query => Set;

    /// <summary>Untracked equivalent, for reads that never get saved back.</summary>
    protected IQueryable<T> ReadQuery => Query.AsNoTracking();

    /// <summary>Default list order. Override where the domain has a natural one.</summary>
    protected virtual IQueryable<T> ApplyDefaultOrder(IQueryable<T> query) => query.OrderBy(e => e.Id);

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await ApplyDefaultOrder(ReadQuery).ToListAsync(ct);

    public virtual void Add(T entity) => Set.Add(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
