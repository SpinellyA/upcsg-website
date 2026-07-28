using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class EventRepository(UpcsgDbContext db) : Repository<GuildEvent>(db), IEventRepository
{
    protected override IQueryable<GuildEvent> ApplyDefaultOrder(IQueryable<GuildEvent> query) =>
        query.OrderBy(e => e.StartDateTime);

    public async Task<IReadOnlyList<GuildEvent>> GetForMonthAsync(
        int year, int month, CancellationToken ct = default)
    {
        // Half-open range on a UTC boundary, so it uses the StartDateTime index rather
        // than forcing a per-row date function.
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        return await ReadQuery
            .Where(e => e.StartDateTime >= start && e.StartDateTime < end)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(ct);
    }
}
