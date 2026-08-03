using Microsoft.EntityFrameworkCore;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Snapshot;

public record GetSnapshotQuery : IQuery<ContentSnapshot>;

/// <summary>
/// Reads the whole public catalogue in one go.
///
/// Five queries rather than one join: these are unrelated aggregates, and joining them
/// would multiply rows for no gain. The dataset is small — a guild's worth of officers,
/// events and merch — so the simple thing is also the fast thing.
/// </summary>
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

        // Through the repository, which includes the variants. Without them every item
        // would snapshot with no sizes and no prices anyone can actually pay.
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
