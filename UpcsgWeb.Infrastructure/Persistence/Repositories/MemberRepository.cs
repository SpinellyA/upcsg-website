using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Repositories;

public class MemberRepository(UpcsgDbContext db) : Repository<Member>(db), IMemberRepository
{
    protected override IQueryable<Member> ApplyDefaultOrder(IQueryable<Member> query) =>
        query.OrderBy(m => m.Category).ThenBy(m => m.DisplayOrder);
}
