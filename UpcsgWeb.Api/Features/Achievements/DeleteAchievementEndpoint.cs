using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Api.Features.Achievements;

public class DeleteAchievementEndpoint(IAchievementRepository achievements, IUnitOfWork uow)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/achievements/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var achievement = await achievements.GetByIdAsync(Route<Guid>("id"), ct);
        if (achievement is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        achievements.Remove(achievement);
        await uow.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
