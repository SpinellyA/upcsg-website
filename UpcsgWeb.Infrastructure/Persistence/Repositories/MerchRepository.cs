using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class MerchRepository(UpcsgDbContext db) : Repository<MerchItem>(db), IMerchRepository
{
    // Variants carry the price a line is actually charged, so they are never optional:
    // without them PriceFor falls back to the base price and undercharges.
    protected override IQueryable<MerchItem> Query => Set.Include("_variants");

    // In-stock items first, so the store and the CMS grid read the same way.
    protected override IQueryable<MerchItem> ApplyDefaultOrder(IQueryable<MerchItem> query) =>
        query.OrderByDescending(m => m.InStock).ThenBy(m => m.Id);

    public async Task<IReadOnlyList<MerchItem>> GetManyAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();

        // Tracked: these are handed to Cart.AddItem and Order.AddLine, which read the
        // live price off the entity.
        return await Query.Where(m => idList.Contains(m.Id)).ToListAsync(ct);
    }
}
