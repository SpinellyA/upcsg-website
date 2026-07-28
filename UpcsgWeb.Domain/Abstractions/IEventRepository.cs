using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Domain.Abstractions;

public interface IEventRepository : IRepository<GuildEvent>
{
    /// <summary>
    /// The site publishes one month at a time, so this is the only list query events
    /// actually need — there is deliberately no "all events" read.
    /// </summary>
    Task<IReadOnlyList<GuildEvent>> GetForMonthAsync(int year, int month, CancellationToken ct = default);
}
