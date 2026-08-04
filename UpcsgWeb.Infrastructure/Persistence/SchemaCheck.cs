using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// Reports migrations that exist in the build but have not been applied to the database.
///
/// This exists because deployment is automatic and migration is not. Pushing a migration
/// ships code that expects columns the database does not have; the deploy succeeds, the
/// health check passes, and the first request touching the new shape fails with a
/// Postgres error about a missing column. Nothing in that sequence points at the cause.
///
/// Checked once at startup rather than per request: it is a property of the deployment,
/// and re-querying on every health ping would add a round trip to answer a question whose
/// answer only changes when someone runs a migration.
/// </summary>
public sealed class SchemaCheck
{
    public IReadOnlyList<string> Pending { get; private set; } = [];

    /// <summary>Null until the check has run, so "unknown" is distinguishable from "none".</summary>
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
            // A database that cannot be reached at startup is not necessarily fatal — the
            // host may still be waking — so this reports rather than throws.
            DatabaseReachable = false;
            logger.LogError(ex, "Could not check the schema version at startup.");
            return;
        }

        if (Pending.Count == 0)
        {
            logger.LogInformation("Schema is up to date.");
            return;
        }

        // Error level on purpose: this is the state where the site looks deployed and is
        // quietly broken, and it needs to stand out in a log nobody reads closely.
        logger.LogError(
            "SCHEMA OUT OF DATE. {Count} migration(s) in this build have not been applied: "
            + "{Migrations}. Requests touching the new shape will fail until "
            + "`./scripts/ef.ps1 database update` is run against this database.",
            Pending.Count,
            string.Join(", ", Pending));
    }
}
