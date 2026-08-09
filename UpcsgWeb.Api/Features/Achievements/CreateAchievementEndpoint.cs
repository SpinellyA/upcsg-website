using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Achievements;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

public class CreateAchievementEndpoint(ISender sender) : Endpoint<AchievementDto, AchievementDto>
{
    public override void Configure()
    {
        Post("/achievements");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(AchievementDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new CreateAchievementCommand(req), ct), ct);
}
