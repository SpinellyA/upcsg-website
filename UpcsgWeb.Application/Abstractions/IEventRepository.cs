using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Application.Abstractions;

public interface IEventRepository : IRepository<GuildEvent>
{
    /// <summary>
    /// Confirmed events starting in the given month. Tentative ones are excluded.
    /// </summary>
    Task<IReadOnlyList<GuildEvent>> GetForMonthAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Announced events without a confirmed date, whatever month they might land in.
    /// </summary>
    Task<IReadOnlyList<GuildEvent>> GetComingSoonAsync(CancellationToken ct = default);
}
