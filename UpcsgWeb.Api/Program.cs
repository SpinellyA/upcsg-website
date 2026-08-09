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
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Infrastructure;
using UpcsgWeb.Infrastructure.Persistence;
using UpcsgWeb.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("Production")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:Production is not configured. Locally, put it in "
        + "UpcsgWeb.Api/appsettings.Development.local.json (git-ignored); in hosting, set "
        + "the ConnectionStrings__Production environment variable.");

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
}

builder.Services.AddApplication();

builder.Services.AddInfrastructure(connectionString);

var media = builder.Services.AddMediaStorage(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    builder.Configuration["Api:SelfUrl"] ?? "http://localhost:5027");

builder.Services.AddScoped<ITokenIssuer, JwtIssuer>();
builder.Services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();

builder.Services.AddSingleton(new SignInOptions
{
    RequiredHostedDomain = builder.Configuration["Google:RequiredHostedDomain"],
});

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
    var isDevelopment = builder.Environment.IsDevelopment();

    var usingBucket = media.UsingBucket;

    o.Filter = endpointType =>
        (isDevelopment || !typeof(IDevelopmentOnlyEndpoint).IsAssignableFrom(endpointType))
        && (!usingBucket || endpointType != typeof(LocalUploadEndpoint));
});
builder.Services.SwaggerDocument(o => o.DocumentSettings = s => s.Title = "UPCSG API");

builder.Services.AddSingleton<SchemaCheck>();

var app = builder.Build();

app.Logger.LogInformation("Media storage: {Provider}", media.Provider);

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SchemaCheck>().RunAsync(
        scope.ServiceProvider.GetRequiredService<UpcsgDbContext>(),
        app.Logger);
}

app.UseStaticFiles();

app.UseCors(CorsPolicies.Frontend);

app.UseDomainExceptionMapping();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";

    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
});

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

app.Run();
