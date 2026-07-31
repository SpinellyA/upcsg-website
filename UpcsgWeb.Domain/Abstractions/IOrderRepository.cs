using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Domain.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetForUserAsync(int userId, CancellationToken ct = default);

    /// <summary>Everything an officer still has to act on.</summary>
    Task<IReadOnlyList<Order>> GetOpenAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);

    /// <summary>
    /// Same set, but tracked, for the caller that intends to change every one of them.
    ///
    /// Separate from <see cref="GetByStatusAsync"/> on purpose: that one is untracked,
    /// so mutating what it returns saves nothing and reports success anyway. A caller
    /// has to ask for write access explicitly.
    /// </summary>
    Task<IReadOnlyList<Order>> GetByStatusForUpdateAsync(OrderStatus status, CancellationToken ct = default);
}
