using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class MerchRepository(UpcsgDbContext db) : Repository<MerchItem>(db), IMerchRepository
{
    protected override IQueryable<MerchItem> Query => Set.Include("_variants");

    protected override IQueryable<MerchItem> ApplyDefaultOrder(IQueryable<MerchItem> query) =>
        query.OrderByDescending(m => m.InStock).ThenBy(m => m.Id);

    public async Task<IReadOnlyList<MerchItem>> GetManyAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();

        return await Query.Where(m => idList.Contains(m.Id)).ToListAsync(ct);
    }
}
