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

        // Tentative events are left out even when they carry a rough date in this month.
        // Placing one on a day would assert a date nobody has confirmed; they surface in
        // the coming-soon list instead. Mirrors GuildEvent.IsScheduled, repeated as an
        // expression because EF cannot translate the property.
        return await ReadQuery
            .Where(e => !e.IsDateTentative
                     && e.StartDateTime >= start
                     && e.StartDateTime < end)
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GuildEvent>> GetComingSoonAsync(CancellationToken ct = default)
    {
        // Undated ones last: an event pencilled in for a month is more informative than one
        // with nothing at all, so it earns the higher slot.
        return await ReadQuery
            .Where(e => e.IsDateTentative || e.StartDateTime == null)
            .OrderBy(e => e.StartDateTime == null)
            .ThenBy(e => e.StartDateTime)
            .ThenBy(e => e.Title)
            .ToListAsync(ct);
    }
}
