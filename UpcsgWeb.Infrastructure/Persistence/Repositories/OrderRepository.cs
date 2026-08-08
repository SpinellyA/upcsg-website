using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class OrderRepository(UpcsgDbContext db) : Repository<Order>(db), IOrderRepository
{
    protected override IQueryable<Order> Query => Set.Include(o => o.Lines);

    protected override IQueryable<Order> ApplyDefaultOrder(IQueryable<Order> query) =>
        query.OrderByDescending(o => o.PlacedAt);

    public async Task<IReadOnlyList<Order>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
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

    public async Task<IReadOnlyList<Order>> GetByStatusForUpdateAsync(
        OrderStatus status, CancellationToken ct = default) =>
        await Query
            .Where(o => o.Status == status)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(ct);
}
