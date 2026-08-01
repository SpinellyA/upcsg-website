using FastEndpoints;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
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
