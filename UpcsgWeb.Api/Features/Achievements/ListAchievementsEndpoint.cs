using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

public class ListAchievementsEndpoint(IAchievementRepository achievements)
    : EndpointWithoutRequest<List<AchievementDto>>
{
    public override void Configure()
    {
        Get("/achievements");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var all = await achievements.GetAllAsync(ct);
        await Send.OkAsync([.. all.Select(a => a.ToDto())], ct);
    }
}
