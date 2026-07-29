using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
    /// Registers image storage: R2 when it's configured, local disk when it isn't.
    ///
    /// Returns the provider that was chosen so the host can say which one it's using at
    /// startup — silently writing to a disk that evaporates on redeploy is the kind of
    /// thing you want announced, not discovered.
    /// </summary>
    public static string AddMediaStorage(
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
            services.AddSingleton<IMediaStore, R2MediaStore>();
            return "Cloudflare R2";
        }

        // Partial configuration is a mistake worth naming rather than quietly ignoring.
        var missing = options.MissingKeys();
        var partial = missing.Count < 5;

        services.AddSingleton<IMediaStore>(sp => new LocalMediaStore(
            sp.GetRequiredService<IOptions<MediaOptions>>(), contentRoot, localBaseUrl));

        return partial
            ? $"local disk — R2 is PARTIALLY configured, missing: {string.Join(", ", missing)}"
            : "local disk (no R2 configuration found)";
    }
}
