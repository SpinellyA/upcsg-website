using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public class AchievementService(HttpClient http, ApiOptions options) : IAchievementService
{
    public async Task<List<AchievementDto>> GetAchievementsAsync()
    {
        // Live when an API is configured; the seed below keeps the public site
        // renderable standalone.
        if (options.IsConfigured)
        {
            return await http.GetFromJsonAsync<List<AchievementDto>>("api/achievements", UpcsgJson.Options) ?? [];
        }

        // Deliberately short. The ExeCom only publishes achievements it can verify, so this
        // starts at the current term and grows backwards as older wins are confirmed â€”
        // it is NOT scoped to a single term. The Hall of Fame page is built to handle both
        // a two-entry list and a multi-year archive, so nothing needs changing as it fills in.
        return
        [
            new AchievementDto
            {
                Id = 1,
                Title = "Champion, UP Cebu Interschool Hackathon",
                Description = "A guilder team took the top prize building an accessibility tool for campus services.",
                Year = 2026,
                Category = "Competition",
            },
            new AchievementDto
            {
                Id = 2,
                Title = "Outstanding Student Organization, College of Science",
                Description = "Recognized for org performance, member programs, and community engagement over the year.",
                Year = 2026,
                Category = "Recognition",
            },
        ];
    }
}
