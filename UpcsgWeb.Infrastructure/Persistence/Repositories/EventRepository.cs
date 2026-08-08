using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class EventRepository(UpcsgDbContext db) : Repository<GuildEvent>(db), IEventRepository
{
    protected override IQueryable<GuildEvent> ApplyDefaultOrder(IQueryable<GuildEvent> query) =>
        query.OrderBy(e => e.StartDateTime);

    public async Task<IReadOnlyList<GuildEvent>> GetForMonthAsync(
        int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        return await ReadQuery
            .Where(e => e.StartDateTime >= start && e.StartDateTime < end)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(ct);
    }
}
