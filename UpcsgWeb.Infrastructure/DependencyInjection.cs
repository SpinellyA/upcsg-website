using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpcsgWeb.Domain.Abstractions;
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
}
