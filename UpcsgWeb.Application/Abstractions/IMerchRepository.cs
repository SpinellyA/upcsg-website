using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Application.Abstractions;

public interface IMerchRepository : IRepository<MerchItem>
{
    Task<IReadOnlyList<MerchItem>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
