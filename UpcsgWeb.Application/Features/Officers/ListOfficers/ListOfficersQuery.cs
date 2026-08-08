using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Officers.ListOfficers;

public record ListOfficersQuery : IQuery<List<OfficerEmailDto>>;

public class ListOfficersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListOfficersQuery, List<OfficerEmailDto>>
{
    public async Task<List<OfficerEmailDto>> Handle(
        ListOfficersQuery query,
        CancellationToken cancellationToken) =>
        await context.OfficerEmails
            .AsNoTracking()
            .OrderBy(o => o.Email)
            .Select(o => new OfficerEmailDto
            {
                Id = o.Id,
                Email = o.Email,
                Note = o.Note,
                AddedAt = o.AddedAt,

                HasSignedIn = context.Users.Any(u => u.Email.ToLower() == o.Email),
            })
            .ToListAsync(cancellationToken);
}
