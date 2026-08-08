using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Application.Abstractions;

public interface IEventRepository : IRepository<GuildEvent>
{
    Task<IReadOnlyList<GuildEvent>> GetForMonthAsync(int year, int month, CancellationToken ct = default);
}
