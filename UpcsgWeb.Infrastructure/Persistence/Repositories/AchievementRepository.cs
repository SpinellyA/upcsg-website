using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class AchievementRepository(UpcsgDbContext db)
    : Repository<Achievement>(db), IAchievementRepository
{
    // Newest first — the Hall of Fame reads as a descending timeline.
    protected override IQueryable<Achievement> ApplyDefaultOrder(IQueryable<Achievement> query) =>
        query.OrderByDescending(a => a.Year).ThenBy(a => a.Title);
}
