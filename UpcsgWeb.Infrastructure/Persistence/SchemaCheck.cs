using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UpcsgWeb.Infrastructure.Persistence;

public sealed class SchemaCheck
{
    public IReadOnlyList<string> Pending { get; private set; } = [];

    public bool? DatabaseReachable { get; private set; }

    public bool IsUpToDate => DatabaseReachable == true && Pending.Count == 0;

    public async Task RunAsync(UpcsgDbContext db, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            Pending = [.. await db.Database.GetPendingMigrationsAsync(ct)];
            DatabaseReachable = true;
        }
        catch (Exception ex)
        {
            DatabaseReachable = false;
            logger.LogError(ex, "Could not check the schema version at startup.");
            return;
        }

        if (Pending.Count == 0)
        {
            logger.LogInformation("Schema is up to date.");
            return;
        }

        logger.LogError(
            "SCHEMA OUT OF DATE. {Count} migration(s) in this build have not been applied: "
            + "{Migrations}. Requests touching the new shape will fail until "
            + "`./scripts/ef.ps1 database update` is run against this database.",
            Pending.Count,
            string.Join(", ", Pending));
    }
}
