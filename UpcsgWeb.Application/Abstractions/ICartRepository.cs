using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Application.Abstractions;

public interface ICartRepository : IRepository<Cart>
{
    /// <summary>
    /// Carts are addressed by owner, never by id — a guilder has exactly one, and no
    /// caller should be able to name someone else's.
    /// </summary>
    Task<Cart?> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
