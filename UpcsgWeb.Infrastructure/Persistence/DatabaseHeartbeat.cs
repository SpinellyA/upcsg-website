using Microsoft.EntityFrameworkCore;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <param name="Reachable">False means the database could not be written to at all.</param>
/// <param name="Wrote">
/// True when this call actually touched the row. False means the throttle window had not
/// elapsed — still healthy, just nothing to do.
/// </param>
/// <param name="LastPingedAt">When the row was last updated, whoever did it.</param>
/// <param name="PingCount">Cumulative, as evidence the pinger has really been running.</param>
public sealed record HeartbeatResult(
    bool Reachable,
    bool Wrote,
    DateTime? LastPingedAt,
    long? PingCount);

public static class DatabaseHeartbeat
{
    /// <summary>
    /// Writes the keep-alive row, at most once per <paramref name="throttle"/>.
    ///
    /// Throttled because /health is public and unauthenticated: without a bound, anyone
    /// could turn a health check into an unlimited write endpoint. Anything inside the
    /// window is answered from the row that is already there, so the cost of a flood is
    /// one cheap read rather than one write each.
    ///
    /// A single UPDATE rather than load-mutate-save. Two pingers firing together would
    /// otherwise read the same count and write the same value back, losing one of the
    /// increments — and the throttle would have a gap between the check and the write.
    /// Here the WHERE clause does the throttling inside the statement, so it is decided
    /// by the database and cannot race.
    /// </summary>
    public static async Task<HeartbeatResult> PingAsync(
        UpcsgDbContext db,
        TimeSpan throttle,
        CancellationToken ct = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow - throttle;

            var rows = await db.Database.ExecuteSqlAsync(
                $"""
                 UPDATE "Heartbeats"
                 SET "LastPingedAt" = NOW() AT TIME ZONE 'UTC',
                     "PingCount" = "PingCount" + 1
                 WHERE "Id" = {Heartbeat.SingletonId}
                   AND "LastPingedAt" < {cutoff}
                 """,
                ct);

            // Read back regardless: it proves the connection works even when the throttle
            // suppressed the write, and gives the pinger something to look at.
            var current = await db.Set<Heartbeat>()
                .AsNoTracking()
                .Where(h => h.Id == Heartbeat.SingletonId)
                .Select(h => new { h.LastPingedAt, h.PingCount })
                .FirstOrDefaultAsync(ct);

            return new HeartbeatResult(
                Reachable: true,
                Wrote: rows > 0,
                LastPingedAt: current?.LastPingedAt,
                PingCount: current?.PingCount);
        }
        catch (Exception)
        {
            // Any failure to reach Postgres is the answer the health check exists to give.
            // Swallowed rather than thrown so the endpoint can return 503 with a body,
            // instead of a 500 that says nothing about which dependency broke.
            return new HeartbeatResult(false, false, null, null);
        }
    }
}
