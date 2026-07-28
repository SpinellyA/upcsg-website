namespace UpcsgWeb.Domain.Abstractions;

/// <summary>
/// Commits everything staged across every repository in the current scope as one unit.
///
/// Implemented by the DbContext, whose change tracker already is one. The value here is
/// not extra transactionality — it's that the domain never has to know EF Core exists.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
