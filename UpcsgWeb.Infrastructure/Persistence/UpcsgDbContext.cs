using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.Settings;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence;

/// <summary>
/// The DbContext doubles as the unit of work — its change tracker already is one, so
/// wrapping it in another transaction object would add indirection without adding
/// behaviour. Implementing IUnitOfWork keeps that fact behind a domain-owned interface.
/// </summary>
public class UpcsgDbContext(DbContextOptions<UpcsgDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<MerchItem> MerchItems => Set<MerchItem>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<GuildEvent> Events => Set<GuildEvent>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpcsgDbContext).Assembly);
}
