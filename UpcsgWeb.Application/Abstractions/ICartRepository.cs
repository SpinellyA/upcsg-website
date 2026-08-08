using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Application.Abstractions;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
