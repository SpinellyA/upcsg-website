using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Infrastructure.Persistence;
using UpcsgWeb.Infrastructure.Persistence.Repositories;

namespace UpcsgWeb.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence. The API composes this without referencing EF Core types
    /// itself — it only ever sees the domain-owned interfaces.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<UpcsgDbContext>(options => options.UseNpgsql(connectionString));

        // Same DbContext instance backs every repository in a request, so all of their
        // staged changes commit together when the unit of work saves.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UpcsgDbContext>());

        // The read side of the same context. Query handlers project from it directly
        // rather than loading aggregates they have no intention of saving.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<UpcsgDbContext>());

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Still registered individually: handlers reach them through IUnitOfWork, but
        // anything that only reads one aggregate can take the repository on its own.
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMerchRepository, MerchRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();

        return services;
    }

    /// <summary>
    /// Which store was chosen.
    ///
    /// <paramref name="UsingBucket"/> is reported as a fact rather than re-derived from
    /// <paramref name="Provider"/>. The host uses it to decide whether the local upload
    /// receiver may be registered at all, and an earlier version tested the display string
    /// with StartsWith("Cloudflare") — which quietly became always-false the moment the
    /// provider was renamed to "Supabase Storage", leaving the local receiver registered
    /// in production.
    /// </summary>
    /// <param name="Provider">Human-readable; for the startup log only.</param>
    /// <param name="UsingBucket">True when bytes go to object storage rather than disk.</param>
    public readonly record struct MediaSetup(string Provider, bool UsingBucket);

    /// <summary>
    /// Registers image storage: the configured bucket when there is one, local disk when
    /// there isn't.
    ///
    /// Returns the provider that was chosen so the host can say which one it's using at
    /// startup — silently writing to a disk that evaporates on redeploy is the kind of
    /// thing you want announced, not discovered.
    /// </summary>
    public static MediaSetup AddMediaStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRoot,
        string localBaseUrl)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));

        var options = new MediaOptions();
        configuration.GetSection(MediaOptions.SectionName).Bind(options);

        if (options.IsConfigured)
        {
            services.AddSingleton<IMediaStore, S3MediaStore>();
            return new MediaSetup(options.DescribeProvider(), UsingBucket: true);
        }

        // Partial configuration is a mistake worth naming rather than quietly ignoring.
        var missing = options.MissingKeys();
        var partial = missing.Count < 5;

        services.AddSingleton<IMediaStore>(sp => new LocalMediaStore(
            sp.GetRequiredService<IOptions<MediaOptions>>(), contentRoot, localBaseUrl));

        return new MediaSetup(
            partial
                ? $"local disk — bucket storage is PARTIALLY configured, missing: {string.Join(", ", missing)}"
                : "local disk (no bucket configuration found)",
            UsingBucket: false);
    }
}
