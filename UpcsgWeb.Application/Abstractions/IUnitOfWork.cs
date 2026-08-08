namespace UpcsgWeb.Application.Abstractions;

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
