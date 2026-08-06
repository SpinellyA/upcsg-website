using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class UserRepository(UpcsgDbContext db) : Repository<AppUser>(db), IUserRepository
{
    public async Task<AppUser?> GetByGoogleSubjectAsync(
        string googleSubject, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct);

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyDictionary<Guid, AppUser>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var distinct = ids.Distinct().ToArray();

        if (distinct.Length == 0)
        {
            // EF would happily translate this to "WHERE id IN ()", but a round trip that
            // cannot match anything is still a round trip on a free-tier database.
            return new Dictionary<Guid, AppUser>();
        }

        return await Query
            .Where(u => distinct.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);
    }
}
