using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Domain.Abstractions;

public interface IUserRepository : IRepository<AppUser>
{
    /// <summary>
    /// The identity lookup used at sign-in. Keyed on Google's subject rather than email,
    /// because an address can be reassigned to a different person.
    /// </summary>
    Task<AppUser?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
}
