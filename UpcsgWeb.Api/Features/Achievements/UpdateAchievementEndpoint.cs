using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Achievements;

public class UpdateAchievementEndpoint(IAchievementRepository achievements, IUnitOfWork uow)
    : Endpoint<AchievementDto, AchievementDto>
{
    public override void Configure()
    {
        Put("/achievements/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(AchievementDto req, CancellationToken ct)
    {
        var achievement = await achievements.GetByIdAsync(Route<int>("id"), ct);
        if (achievement is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            achievement.Update(req.Title, req.Description, req.Year, req.Category, req.ImageUrl);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(achievement.ToDto(), ct);
    }
}
