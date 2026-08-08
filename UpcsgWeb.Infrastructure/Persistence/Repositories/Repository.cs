using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public abstract class Repository<T>(UpcsgDbContext db) : IRepository<T>
    where T : AggregateRoot
{
    protected UpcsgDbContext Db { get; } = db;

    protected DbSet<T> Set => Db.Set<T>();

    protected virtual IQueryable<T> Query => Set;

    protected IQueryable<T> ReadQuery => Query.AsNoTracking();

    protected virtual IQueryable<T> ApplyDefaultOrder(IQueryable<T> query) => query.OrderBy(e => e.Id);

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await ApplyDefaultOrder(ReadQuery).ToListAsync(ct);

    public virtual void Add(T entity) => Set.Add(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
