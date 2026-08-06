using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

public interface IUserRepository : IRepository<AppUser>
{
    /// <summary>
    /// The identity lookup used at sign-in. Keyed on Google's subject rather than email,
    /// because an address can be reassigned to a different person.
    /// </summary>
    Task<AppUser?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// The users behind a batch of orders, keyed by id.
    ///
    /// Exists so the orders board can name guilders in one query rather than one per row.
    /// Order references its user by id only — they are separate aggregates, and giving
    /// Order a navigation property would let a caller edit a user through an order — so
    /// joining them is a read-side concern and belongs here.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, AppUser>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default);
}
