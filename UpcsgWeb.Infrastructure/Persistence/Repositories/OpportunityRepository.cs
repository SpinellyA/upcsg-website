using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class OpportunityRepository(UpcsgDbContext db)
    : Repository<Opportunity>(db), IOpportunityRepository
{
    protected override IQueryable<Opportunity> ApplyDefaultOrder(IQueryable<Opportunity> query) =>
        query
            .OrderByDescending(o => o.IsFeatured)
            .ThenBy(o => o.ClosesAt == null)
            .ThenBy(o => o.ClosesAt)
            .ThenByDescending(o => o.CreatedAt);

    public async Task<IReadOnlyList<Opportunity>> GetOpenAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await ApplyDefaultOrder(ReadQuery.Where(o => o.ClosesAt == null || o.ClosesAt >= now))
            .ToListAsync(ct);
    }
}
