using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic CRUD only. The one thing worth keeping is the roster order, which matches
/// the index on (Category, DisplayOrder) and the order the About page renders in.
/// </summary>
public class MemberRepository(UpcsgDbContext db) : Repository<Member>(db), IMemberRepository
{
    protected override IQueryable<Member> ApplyDefaultOrder(IQueryable<Member> query) =>
        query.OrderBy(m => m.Category).ThenBy(m => m.DisplayOrder);
}
