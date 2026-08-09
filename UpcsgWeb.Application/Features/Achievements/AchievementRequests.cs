using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Achievements;

public record GetAchievementQuery(Guid Id) : IQuery<AchievementDto?>;

public record ListAchievementsQuery : IQuery<List<AchievementDto>>;

public record CreateAchievementCommand(AchievementDto Achievement) : ICommand<AchievementDto>;

public record UpdateAchievementCommand(Guid Id, AchievementDto Achievement) : ICommand<AchievementDto>;

public record DeleteAchievementCommand(Guid Id) : ICommand;
