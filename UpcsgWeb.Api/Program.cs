using System.Text;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Features.Dev;
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

// Endpoint for the uptime pinger. Touches the DB, so a warm process sitting on a cold
// Neon connection still reports unhealthy.
app.MapGet("/health", async (UpcsgDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return ok ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503);
});

app.Run();
