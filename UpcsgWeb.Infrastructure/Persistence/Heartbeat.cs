namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// A single row that the health check touches, so an uptime pinger keeps the database
/// counted as active and not just the web service.
///
/// Deliberately not a domain type. The guild has no concept of a heartbeat — it exists
/// because a free-tier Postgres project pauses when nothing writes to it, which is a
/// hosting fact rather than a business rule. Putting it in Domain would mean the model
/// carried a row that exists only to defeat an idle timer.
///
/// One row, fixed id, seeded by the migration. Nothing ever inserts or deletes here.
/// </summary>
public class Heartbeat
{
    /// <summary>The only row. Fixed so the update never has to look it up first.</summary>
    public static readonly Guid SingletonId = new("11111111-2222-4333-8444-555555555555");

    public Guid Id { get; set; }

    public DateTime LastPingedAt { get; set; }

    /// <summary>
    /// Cumulative, and only really useful as evidence: if the site went down over a
    /// break, this says whether the pinger was actually running.
    /// </summary>
    public long PingCount { get; set; }
}
