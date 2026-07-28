using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Domain.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetForUserAsync(int userId, CancellationToken ct = default);

    /// <summary>Everything an officer still has to act on.</summary>
    Task<IReadOnlyList<Order>> GetOpenAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);
}
