using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAchievementService
{
    Task<List<AchievementDto>> GetAchievementsAsync();

    /// <summary>
    /// One entry by id, for the detail article. Fetched directly rather than filtered out
    /// of the full list, so a shared link survives the record growing.
    /// </summary>
    Task<AchievementDto?> GetAchievementAsync(int id);
}
