using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class AchievementRepository(UpcsgDbContext db)
    : Repository<Achievement>(db), IAchievementRepository
{
    protected override IQueryable<Achievement> ApplyDefaultOrder(IQueryable<Achievement> query) =>
        query.OrderByDescending(a => a.Year).ThenByDescending(a => a.CreatedAt);
}
