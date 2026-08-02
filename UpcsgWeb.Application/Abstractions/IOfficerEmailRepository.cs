using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

public interface IOfficerEmailRepository : IRepository<OfficerEmail>
{
    /// <summary>Normalised lookup. Callers must not pass raw text from a token.</summary>
    Task<OfficerEmail?> GetByEmailAsync(string normalisedEmail, CancellationToken ct = default);

    Task<bool> IsOfficerAsync(string normalisedEmail, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);
}
