using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class OfficerEmailRepository(UpcsgDbContext db)
    : Repository<OfficerEmail>(db), IOfficerEmailRepository
{
    public async Task<OfficerEmail?> GetByEmailAsync(
        string normalisedEmail, CancellationToken ct = default) =>
        await Query.FirstOrDefaultAsync(o => o.Email == normalisedEmail, ct);

    /// <summary>
    /// AnyAsync rather than loading the row: this runs on every sign-in and only the
    /// yes/no matters.
    /// </summary>
    public async Task<bool> IsOfficerAsync(string normalisedEmail, CancellationToken ct = default) =>
        await Set.AsNoTracking().AnyAsync(o => o.Email == normalisedEmail, ct);

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().CountAsync(ct);
}
