using FastEndpoints;
using UpcsgWeb.Infrastructure.Persistence;

namespace UpcsgWeb.Api.Features.Health;

/// <summary>
/// Endpoint for the uptime pinger, and the one thing keeping both halves of the free tier
/// alive: the web service sleeps when nothing calls it, and the Postgres project pauses
/// when nothing writes to it. A read would wake only the first, so this writes.
///
/// The write is throttled inside the SQL rather than here, so leaving this public and
/// unauthenticated does not make it an unlimited write endpoint.
/// </summary>
public class HealthEndpoint(UpcsgDbContext db, SchemaCheck schema)
    : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        // HEAD as well as GET. UptimeRobot - and most uptime monitors - send HEAD by
        // default, and ASP.NET Core does not answer HEAD for a GET-only route: it returns
        // 405 with "Allow: GET". That reads as an outage on every check, which is a monitor
        // that reports the opposite of the truth.
        //
        // HEAD still runs this handler and still writes the heartbeat; only the body is
        // discarded. So a HEAD-based monitor keeps the database awake exactly like a GET.
        Verbs("GET", "HEAD");
        Routes("/health");

        AllowAnonymous();

        // Every other endpoint sits under /api. This one must not: Render's healthCheckPath
        // and the uptime monitor are both configured against /health, and moving it would
        // silently fail both.
        RoutePrefixOverride(string.Empty);

        Summary(s => s.Summary = "Liveness probe and database keep-alive.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var beat = await DatabaseHeartbeat.PingAsync(db, TimeSpan.FromMinutes(1), ct);

        if (!beat.Reachable)
        {
            // 503, not 500: the API is up, its database is not. A pinger that treats every
            // non-200 the same still alerts, and anyone reading the body learns which it was.
            await Send.ResponseAsync(
                new HealthResponse { Status = "unhealthy", Database = "unreachable" },
                StatusCodes.Status503ServiceUnavailable,
                ct);

            return;
        }

        await Send.OkAsync(new HealthResponse
        {
            Status = "healthy",
            Database = "reachable",

            // Still 200: the database answers and most of the site works. But a deployment
            // carrying unapplied migrations is a half-finished release, and this is where
            // someone looks when a page starts failing right after a push.
            Schema = schema.IsUpToDate ? "up to date" : "MIGRATIONS PENDING",
            PendingMigrations = schema.Pending,

            // False just means another request already wrote inside the throttle window.
            KeptAlive = beat.Wrote,
            LastKeepAlive = beat.LastPingedAt,
            PingCount = beat.PingCount,
        }, ct);
    }
}

/// <summary>
/// Typed rather than an anonymous object, which is what the minimal-api version returned.
/// The shape is what an uptime monitor's keyword check and any future dashboard read, so
/// it is worth being able to find every field by name.
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public IReadOnlyList<string>? PendingMigrations { get; set; }
    public bool? KeptAlive { get; set; }
    public DateTime? LastKeepAlive { get; set; }
    public long? PingCount { get; set; }
}
