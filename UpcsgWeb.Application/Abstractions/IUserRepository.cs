using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

public interface IUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken ct = default);

    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, AppUser>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default);
}
