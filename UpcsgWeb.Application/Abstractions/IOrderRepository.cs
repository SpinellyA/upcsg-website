using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetOpenAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetByStatusForUpdateAsync(OrderStatus status, CancellationToken ct = default);
}
