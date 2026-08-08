using FastEndpoints;
using UpcsgWeb.Infrastructure.Persistence;

namespace UpcsgWeb.Api.Features.Health;

public class HealthEndpoint(UpcsgDbContext db, SchemaCheck schema)
    : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Verbs("GET", "HEAD");
        Routes("/health");

        AllowAnonymous();

        RoutePrefixOverride(string.Empty);

        Summary(s => s.Summary = "Liveness probe and database keep-alive.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var beat = await DatabaseHeartbeat.PingAsync(db, TimeSpan.FromMinutes(1), ct);

        if (!beat.Reachable)
        {
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

            Schema = schema.IsUpToDate ? "up to date" : "MIGRATIONS PENDING",
            PendingMigrations = schema.Pending,

            KeptAlive = beat.Wrote,
            LastKeepAlive = beat.LastPingedAt,
            PingCount = beat.PingCount,
        }, ct);
    }
}

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
