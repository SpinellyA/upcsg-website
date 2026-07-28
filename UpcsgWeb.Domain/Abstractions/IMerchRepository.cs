using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Domain.Abstractions;

public interface IMerchRepository : IRepository<MerchItem>
{
    /// <summary>
    /// Fetches several at once, tracked, so building an order from a cart is one query
    /// rather than one per line.
    /// </summary>
    Task<IReadOnlyList<MerchItem>> GetManyAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
