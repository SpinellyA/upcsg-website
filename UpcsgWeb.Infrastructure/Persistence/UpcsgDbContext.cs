using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.Settings;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Infrastructure.Persistence.Repositories;

namespace UpcsgWeb.Infrastructure.Persistence;

public class UpcsgDbContext(
    DbContextOptions<UpcsgDbContext> options,
    IDomainEventDispatcher? dispatcher = null)
    : DbContext(options), IUnitOfWork, IApplicationDbContext
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<MerchItem> MerchItems => Set<MerchItem>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OfficerEmail> OfficerEmails => Set<OfficerEmail>();
    public DbSet<GuildEvent> Events => Set<GuildEvent>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    private ICartRepository? _cartRepository;
    private IOrderRepository? _orderRepository;
    private IMerchRepository? _merchRepository;
    private IUserRepository? _userRepository;
    private IOfficerEmailRepository? _officerEmailRepository;
    private IEventRepository? _eventRepository;
    private IMemberRepository? _memberRepository;
    private IAchievementRepository? _achievementRepository;
    private IOpportunityRepository? _opportunityRepository;
    private ISiteSettingsRepository? _siteSettingsRepository;

    ICartRepository IUnitOfWork.Carts => _cartRepository ??= new CartRepository(this);
    IOrderRepository IUnitOfWork.Orders => _orderRepository ??= new OrderRepository(this);
    IMerchRepository IUnitOfWork.Merch => _merchRepository ??= new MerchRepository(this);
    IUserRepository IUnitOfWork.Users => _userRepository ??= new UserRepository(this);
    IOfficerEmailRepository IUnitOfWork.OfficerEmails => _officerEmailRepository ??= new OfficerEmailRepository(this);
    IEventRepository IUnitOfWork.Events => _eventRepository ??= new EventRepository(this);
    IMemberRepository IUnitOfWork.Members => _memberRepository ??= new MemberRepository(this);
    IAchievementRepository IUnitOfWork.Achievements => _achievementRepository ??= new AchievementRepository(this);

    IOpportunityRepository IUnitOfWork.Opportunities =>
        _opportunityRepository ??= new OpportunityRepository(this);
    ISiteSettingsRepository IUnitOfWork.SiteSettings => _siteSettingsRepository ??= new SiteSettingsRepository(this);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UpcsgDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var roots = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(root => root.DomainEvents.Count > 0)
            .ToList();

        var written = await base.SaveChangesAsync(cancellationToken);

        if (dispatcher is not null && roots.Count > 0)
        {
            await dispatcher.DispatchAsync(roots, cancellationToken);
        }

        return written;
    }
}
