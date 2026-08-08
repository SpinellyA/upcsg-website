using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

public interface IOfficerEmailRepository : IRepository<OfficerEmail>
{
    Task<OfficerEmail?> GetByEmailAsync(string normalisedEmail, CancellationToken ct = default);

    Task<bool> IsOfficerAsync(string normalisedEmail, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);
}
