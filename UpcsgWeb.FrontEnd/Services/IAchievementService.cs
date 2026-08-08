using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAchievementService
{
    Task<List<AchievementDto>> GetAchievementsAsync();

    Task<AchievementDto?> GetAchievementAsync(Guid id);
}
