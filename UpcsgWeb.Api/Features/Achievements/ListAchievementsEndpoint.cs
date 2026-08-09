using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Achievements;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

public class ListAchievementsEndpoint(ISender sender) : EndpointWithoutRequest<List<AchievementDto>>
{
    public override void Configure()
    {
        Get("/achievements");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListAchievementsQuery(), ct), ct);
}
