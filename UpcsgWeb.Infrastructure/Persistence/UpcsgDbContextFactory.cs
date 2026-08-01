using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// Lets the EF tools build the model without starting the API.
///
/// Scaffolding a migration only needs the provider, not a reachable server, so the
/// placeholder below is enough for `migrations add`. Applying one needs the real thing:
/// set UPCSG_CONNECTION for `database update`, which also keeps the Neon credentials out
/// of the repository.
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
                + "Read it from the API's user-secrets (ConnectionStrings:Neon) and set it "
                + "for the command only.");
        }

        var options = new DbContextOptionsBuilder<UpcsgDbContext>()
            .UseNpgsql(connection)
            .Options;

        // No dispatcher: design time never saves, so nothing can be published.
        return new UpcsgDbContext(options);
    }
}
