using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.Settings;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<OfficerEmail> OfficerEmails { get; }
    DbSet<MerchItem> MerchItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<Cart> Carts { get; }
    DbSet<GuildEvent> Events { get; }
    DbSet<Member> Members { get; }
    DbSet<Achievement> Achievements { get; }
    DbSet<SiteSettings> SiteSettings { get; }
}
