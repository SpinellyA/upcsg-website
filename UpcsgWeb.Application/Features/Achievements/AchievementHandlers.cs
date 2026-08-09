using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;
using DomainAchievement = UpcsgWeb.Domain.Content.Achievement;

namespace UpcsgWeb.Application.Features.Achievements;

public class GetAchievementQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetAchievementQuery, AchievementDto?>
{
    public async Task<AchievementDto?> Handle(GetAchievementQuery query, CancellationToken ct)
    {
        var found = await uow.Achievements.GetByIdAsync(query.Id, ct);
        return found?.ToDto();
    }
}

public class ListAchievementsQueryHandler(IUnitOfWork uow)
    : IQueryHandler<ListAchievementsQuery, List<AchievementDto>>
{
    public async Task<List<AchievementDto>> Handle(ListAchievementsQuery query, CancellationToken ct)
    {
        var all = await uow.Achievements.GetAllAsync(ct);
        return [.. all.Select(a => a.ToDto())];
    }
}

public class CreateAchievementCommandHandler(IUnitOfWork uow)
    : ICommandHandler<CreateAchievementCommand, AchievementDto>
{
    public async Task<AchievementDto> Handle(CreateAchievementCommand command, CancellationToken ct)
    {
        var dto = command.Achievement;

        var achievement = DomainAchievement.Create(dto.Title, dto.Description, dto.Year, dto.Category);
        achievement.Update(dto.Title, dto.Description, dto.Year, dto.Category, dto.ImageUrl);

        uow.Achievements.Add(achievement);
        await uow.SaveChangesAsync(ct);

        return achievement.ToDto();
    }
}

public class UpdateAchievementCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateAchievementCommand, AchievementDto>
{
    public async Task<AchievementDto> Handle(UpdateAchievementCommand command, CancellationToken ct)
    {
        var achievement = await uow.Achievements.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That achievement");

        var dto = command.Achievement;
        achievement.Update(dto.Title, dto.Description, dto.Year, dto.Category, dto.ImageUrl);

        await uow.SaveChangesAsync(ct);

        return achievement.ToDto();
    }
}

public class DeleteAchievementCommandHandler(IUnitOfWork uow)
    : ICommandHandler<DeleteAchievementCommand>
{
    public async Task Handle(DeleteAchievementCommand command, CancellationToken ct)
    {
        var achievement = await uow.Achievements.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That achievement");

        uow.Achievements.Remove(achievement);
        await uow.SaveChangesAsync(ct);
    }
}
