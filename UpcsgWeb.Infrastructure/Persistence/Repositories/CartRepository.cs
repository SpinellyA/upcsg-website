using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class CartRepository(UpcsgDbContext db) : Repository<Cart>(db), ICartRepository
{
    protected override IQueryable<Cart> Query => Set.Include(c => c.Lines);

    // Tracked: callers mutate the cart and the unit of work persists it.
    public async Task<Cart?> GetForUserAsync(int userId, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(c => c.UserId == userId, ct);
}
