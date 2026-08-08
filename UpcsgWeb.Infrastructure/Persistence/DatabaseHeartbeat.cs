using Microsoft.EntityFrameworkCore;

namespace UpcsgWeb.Infrastructure.Persistence;

public sealed record HeartbeatResult(
    bool Reachable,
    bool Wrote,
    DateTime? LastPingedAt,
    long? PingCount);

public static class DatabaseHeartbeat
{
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
            return new HeartbeatResult(false, false, null, null);
        }
    }
}
