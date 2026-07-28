using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Api.Features.Achievements;

public class DeleteAchievementEndpoint(IAchievementRepository achievements, IUnitOfWork uow)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/achievements/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var achievement = await achievements.GetByIdAsync(Route<int>("id"), ct);
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
