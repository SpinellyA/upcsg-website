using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;
using DomainAchievement = UpcsgWeb.Domain.Content.Achievement;

namespace UpcsgWeb.Api.Features.Achievements;

public class CreateAchievementEndpoint(IAchievementRepository achievements, IUnitOfWork uow)
    : Endpoint<AchievementDto, AchievementDto>
{
    public override void Configure()
    {
        Post("/achievements");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(AchievementDto req, CancellationToken ct)
    {
        DomainAchievement achievement;
        try
        {
            achievement = DomainAchievement.Create(req.Title, req.Description, req.Year, req.Category);
            achievement.Update(req.Title, req.Description, req.Year, req.Category, req.ImageUrl);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        achievements.Add(achievement);
        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(achievement.ToDto(), ct);
    }
}
