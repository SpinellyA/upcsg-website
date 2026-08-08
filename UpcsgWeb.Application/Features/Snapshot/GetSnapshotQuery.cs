using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Snapshot;

public record GetSnapshotQuery : IQuery<ContentSnapshot>;

public class GetSnapshotQueryHandler(IApplicationDbContext context, IUnitOfWork uow)
    : IQueryHandler<GetSnapshotQuery, ContentSnapshot>
{
    public async Task<ContentSnapshot> Handle(
        GetSnapshotQuery query,
        CancellationToken cancellationToken)
    {
        var members = await context.Members
            .AsNoTracking()
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync(cancellationToken);

        var events = await context.Events
            .AsNoTracking()
            .OrderBy(e => e.StartDateTime)
            .ToListAsync(cancellationToken);

        var achievements = await context.Achievements
            .AsNoTracking()
            .OrderByDescending(a => a.Year)
            .ToListAsync(cancellationToken);

        var merch = await uow.Merch.GetAllAsync(cancellationToken);

        var settings = await uow.SiteSettings.GetAsync(cancellationToken);

        return new ContentSnapshot
        {
            GeneratedAt = DateTime.UtcNow,
            Members = [.. members.Select(m => m.ToDto())],
            Events = [.. events.Select(e => e.ToDto())],
            Achievements = [.. achievements.Select(a => a.ToDto())],
            Merch = [.. merch.Select(m => m.ToDto())],
            Settings = settings.ToDto(),
        };
    }
}
