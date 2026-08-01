using FastEndpoints;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

/// <summary>
/// Backs the Hall of Fame detail page. Fetching by id rather than filtering the full
/// list keeps a shared link working no matter how long the record grows.
/// </summary>
public class GetAchievementEndpoint(IAchievementRepository achievements)
    : EndpointWithoutRequest<AchievementDto>
{
    public override void Configure()
    {
        Get("/achievements/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await achievements.GetByIdAsync(Route<Guid>("id"), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found.ToDto(), ct);
    }
}
