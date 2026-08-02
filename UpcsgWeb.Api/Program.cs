using System.Text;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Features.Dev;
using UpcsgWeb.Api;
using UpcsgWeb.Api.Features.Media;
using UpcsgWeb.Application;
using UpcsgWeb.Infrastructure;
using UpcsgWeb.Infrastructure.Persistence;
using UpcsgWeb.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ------------------------------------------------------------------
// Locally these come from user-secrets; on Render from environment variables
// (ConnectionStrings__Neon, Jwt__SigningKey). Neither is ever committed.
var connectionString = builder.Configuration.GetConnectionString("Neon")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Neon is not configured. Set it via user-secrets locally or an env var in hosting.");

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    // HS256 needs >= 256 bits of key material. Failing here beats failing at the first
    // sign-in, which is a confusing place to discover a config problem.
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
}

// --- Services -----------------------------------------------------------------------
// Application first: Infrastructure's DomainEventDispatcher takes MediatR's IPublisher,
// so leaving this out makes every repository registration fail to construct.
builder.Services.AddApplication();

// Persistence is composed behind one call; the API references no EF Core types itself
// and talks only to the repository interfaces the Domain owns.
builder.Services.AddInfrastructure(connectionString);

// Image storage: R2 when configured, local disk otherwise. The chosen provider is
// reported at startup — writing to Render's ephemeral disk without noticing is exactly
// the failure this announcement exists to prevent.
var mediaProvider = builder.Services.AddMediaStorage(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    builder.Configuration["Api:SelfUrl"] ?? "http://localhost:5027");

builder.Services.AddScoped<JwtIssuer>();
builder.Services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = JwtIssuer.Issuer,
            ValidAudience = JwtIssuer.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.ExeCom, policy => policy.RequireRole(UpcsgRoles.Admin));
});

// The browser blocks cross-origin calls unless the API names the caller explicitly.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5005", "https://localhost:7030"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Frontend, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddFastEndpoints(o =>
{
    // Endpoints marked IDevelopmentOnlyEndpoint are excluded from the registry entirely
    // outside Development, so the route does not exist rather than merely refusing.
    // See Features/Dev/DevSignInEndpoint.cs — it can mint admin tokens.
    //
    // Matched by type, not by namespace string: the previous version matched a namespace
    // that a refactor renamed, which silently disabled the guard.
    var isDevelopment = builder.Environment.IsDevelopment();

    // The local upload receiver only exists when there is no bucket to presign against.
    // With R2 configured it must not be registered at all, or it would stand as a second,
    // unsigned way to put bytes on the server.
    var usingBucket = mediaProvider.StartsWith("Cloudflare", StringComparison.Ordinal);

    // Composed once: EndpointDiscoveryOptions.Filter is set-only, so it cannot be layered.
    o.Filter = endpointType =>
        (isDevelopment || !typeof(IDevelopmentOnlyEndpoint).IsAssignableFrom(endpointType))
        && (!usingBucket || endpointType != typeof(LocalUploadEndpoint));
});
builder.Services.SwaggerDocument(o => o.DocumentSettings = s => s.Title = "UPCSG API");

var app = builder.Build();

app.Logger.LogInformation("Media storage: {Provider}", mediaProvider);

// --- Pipeline -----------------------------------------------------------------------
// Serves wwwroot/media for the local store. Harmless with R2 configured — nothing is
// written there — and it is what makes uploads viewable in development.
app.UseStaticFiles();

// CORS goes before auth so preflight OPTIONS requests aren't rejected as unauthorised.
app.UseCors(CorsPolicies.Frontend);

// Outside the endpoints so it catches rules thrown from anywhere in the handler chain,
// but inside CORS so the error response still carries the headers the browser needs to
// let the frontend read it.
app.UseDomainExceptionMapping();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";

    // Enums travel as names, not ordinals. Numeric enums make the API unreadable and,
    // worse, silently shift meaning if a value is ever inserted mid-enum — the same
    // reason OrderStatus is stored as text in Postgres.
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

// Endpoint for the uptime pinger, and the one thing keeping both halves of the free tier
// alive: the web service sleeps when nothing calls it, and the Postgres project pauses
// when nothing writes to it. A read would wake only the first, so this writes.
//
// The write is throttled inside the SQL rather than here, so /health staying public and
// unauthenticated does not make it an unlimited write endpoint.
app.MapGet("/health", async (UpcsgDbContext db, CancellationToken ct) =>
{
    var beat = await DatabaseHeartbeat.PingAsync(db, TimeSpan.FromMinutes(1), ct);

    if (!beat.Reachable)
    {
        // 503, not 500: the API is up, its database is not. A pinger that treats every
        // non-200 the same still alerts, and anyone reading the body learns which it was.
        return Results.Json(
            new { status = "unhealthy", database = "unreachable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "healthy",
        database = "reachable",

        // False just means another request already wrote inside the throttle window.
        keptAlive = beat.Wrote,
        lastKeepAlive = beat.LastPingedAt,
        pingCount = beat.PingCount,
    });
});

app.Run();
