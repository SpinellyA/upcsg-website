using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Achievements;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

public class GetAchievementEndpoint(ISender sender) : EndpointWithoutRequest<AchievementDto>
{
    public override void Configure()
    {
        Get("/achievements/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await sender.Send(new GetAchievementQuery(Route<Guid>("id")), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found, ct);
    }
}
