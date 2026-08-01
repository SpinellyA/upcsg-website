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
}
