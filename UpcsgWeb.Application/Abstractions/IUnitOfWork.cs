namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// The write side: the repositories a command may reach, and the single commit that
/// closes over all of them.
///
/// Exposing the repositories here rather than injecting each one separately is what
/// makes the transaction boundary obvious — everything a handler touches came from one
/// unit of work, so one SaveChangesAsync is all it can mean. Implemented by the
/// DbContext, whose change tracker already is a unit of work; the value is that the
/// application layer never has to know EF Core exists.
///
/// Queries do not come through here. They read <see cref="IApplicationDbContext"/>.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IOfficerEmailRepository OfficerEmails { get; }
    IMerchRepository Merch { get; }
    IOrderRepository Orders { get; }
    ICartRepository Carts { get; }
    IEventRepository Events { get; }
    IMemberRepository Members { get; }
    IAchievementRepository Achievements { get; }
    ISiteSettingsRepository SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
