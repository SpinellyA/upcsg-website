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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<UpcsgDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UpcsgDbContext>());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<UpcsgDbContext>());

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

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

    public readonly record struct MediaSetup(string Provider, bool UsingBucket);

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

        var missing = options.MissingKeys();
        var partial = missing.Count < 5;

        services.AddSingleton<IMediaStore>(sp => new LocalMediaStore(
            sp.GetRequiredService<IOptions<MediaOptions>>(), contentRoot, localBaseUrl));

        return new MediaSetup(
            partial
                ? $"local disk, but bucket storage is PARTIALLY configured, missing: {string.Join(", ", missing)}"
                : "local disk (no bucket configuration found)",
            UsingBucket: false);
    }
}
