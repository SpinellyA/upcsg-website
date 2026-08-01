using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.Settings;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// Read access for queries, deliberately separate from the write side.
///
/// A query handler projects straight to a DTO with AsNoTracking and never loads an
/// aggregate: reading a whole Order to render one row costs more and, worse, hands the
/// caller something mutable that nobody intends to save. Commands go through
/// <see cref="IUnitOfWork"/> and the repositories instead.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<MerchItem> MerchItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<Cart> Carts { get; }
    DbSet<GuildEvent> Events { get; }
    DbSet<Member> Members { get; }
    DbSet<Achievement> Achievements { get; }
    DbSet<SiteSettings> SiteSettings { get; }
}
