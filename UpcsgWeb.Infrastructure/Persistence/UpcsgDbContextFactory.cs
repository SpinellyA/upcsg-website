using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// Lets the EF tools build the model without starting the API.
///
/// Scaffolding a migration only needs the provider, not a reachable server. Applying one
/// needs the real thing, which comes from UPCSG_CONNECTION so the credentials stay out of
/// the repository.
///
/// Use scripts/ef.ps1 rather than setting that variable by hand. Pasting a connection
/// string into PowerShell truncates it at the first ';' if unquoted, and at a '#' in the
/// password even when quoted with double quotes — producing a "Format of the
/// initialization string does not conform to specification" error that points at a
/// perfectly valid string.
/// </summary>
public class UpcsgDbContextFactory : IDesignTimeDbContextFactory<UpcsgDbContext>
{
    public UpcsgDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("UPCSG_CONNECTION");

        if (string.IsNullOrWhiteSpace(connection))
        {
            // EF prefers this factory over the API's own configuration, so a silent
            // localhost fallback here would point `database drop` at the wrong server
            // while reporting success. Refuse instead.
            throw new InvalidOperationException(
                "UPCSG_CONNECTION is not set. Design-time EF commands need the connection "
                + "string explicitly so they cannot quietly target the wrong database. "
                + "Run scripts/ef.ps1 instead, which reads it from the API's user-secrets "
                + "(ConnectionStrings:Production) and sets it for the one command.");
        }

        var options = new DbContextOptionsBuilder<UpcsgDbContext>()
            .UseNpgsql(connection)
            .Options;

        // No dispatcher: design time never saves, so nothing can be published.
        return new UpcsgDbContext(options);
    }
}
