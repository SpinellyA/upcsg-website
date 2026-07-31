using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class OrderRepository(UpcsgDbContext db) : Repository<Order>(db), IOrderRepository
{
    // Declared once here so every inherited and derived query loads the aggregate whole.
    // An Order without its lines would report a total of zero.
    protected override IQueryable<Order> Query => Set.Include(o => o.Lines);

    protected override IQueryable<Order> ApplyDefaultOrder(IQueryable<Order> query) =>
        query.OrderByDescending(o => o.PlacedAt);

    public async Task<IReadOnlyList<Order>> GetForUserAsync(int userId, CancellationToken ct = default) =>
        await ReadQuery
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetOpenAsync(CancellationToken ct = default) =>
        await ReadQuery
            .Where(o => o.Status != OrderStatus.Received && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status, CancellationToken ct = default) =>
        await ReadQuery
            .Where(o => o.Status == status)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(ct);

    // Query, not ReadQuery: the caller is going to mutate these and expect a save.
    public async Task<IReadOnlyList<Order>> GetByStatusForUpdateAsync(
        OrderStatus status, CancellationToken ct = default) =>
        await Query
            .Where(o => o.Status == status)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(ct);
}
