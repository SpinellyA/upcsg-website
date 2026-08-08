using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class CartRepository(UpcsgDbContext db) : Repository<Cart>(db), ICartRepository
{
    protected override IQueryable<Cart> Query => Set.Include(c => c.Lines);

    public async Task<Cart?> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(c => c.UserId == userId, ct);
}
