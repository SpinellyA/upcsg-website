using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public class AchievementService(HttpClient http, ApiOptions options, ISnapshotService snapshots)
    : IAchievementService
{
    // Live when reachable, then the committed snapshot, then the built-in seed.
    public Task<List<AchievementDto>> GetAchievementsAsync() =>
        LiveOrSnapshot.ReadAsync(
            options,
            snapshots,
            async () => await http.GetFromJsonAsync<List<AchievementDto>>("api/achievements", UpcsgJson.Options) ?? [],
            snapshot => snapshot.Achievements,
            SeedData);

    public async Task<AchievementDto?> GetAchievementAsync(Guid id)
    {
        if (!options.IsConfigured)
        {
            return (await GetAchievementsAsync()).FirstOrDefault(a => a.Id == id);
        }

        try
        {
            // A bad id in the URL is an ordinary answer, not an exception for the page.
            var response = await http.GetAsync($"api/achievements/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AchievementDto>(UpcsgJson.Options);
        }
        catch (HttpRequestException)
        {
            // Unreachable rather than not found — the list path knows how to fall back.
            return (await GetAchievementsAsync()).FirstOrDefault(a => a.Id == id);
        }
    }

    /// <summary>
    /// Deliberately short. The ExeCom only publishes achievements it can verify, so this
    /// starts at the current term and grows backwards as older wins are confirmed — it is
    /// NOT scoped to a single term. The Hall of Fame is built to handle both a two-entry
    /// list and a multi-year archive, so nothing needs changing as it fills in.
    ///
    /// Descriptions are written as full articles: the detail page renders every paragraph,
    /// and the cards show the first one as the standfirst.
    /// </summary>
    private static List<AchievementDto> SeedData() =>
    [
        new AchievementDto
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            Title = "Champion, UP Cebu Interschool Hackathon",
            Year = 2026,
            Category = "Competition",
            Description =
                """
                A guilder team took the top prize at the interschool hackathon with an
                accessibility tool for campus services, beating out fourteen other teams over
                a thirty-six hour build.

                The brief was open-ended: build something that makes campus life measurably
                better. Most teams went for scheduling and food delivery. Ours went narrower
                and harder — a screen-reader-first interface for the campus service portal,
                built after talking to students who actually rely on one.

                The judges singled out the decision to test with real users mid-build rather
                than demo to an empty room. Two rounds of feedback landed before submission,
                and the second round changed the navigation model entirely.

                It is the guild's first win at this competition. The build is open source and
                the team has been talking with the college about adopting parts of it.
                """,
        },
        new AchievementDto
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            Title = "Outstanding Student Organization, College of Science",
            Year = 2026,
            Category = "Recognition",
            Description =
                """
                UPCSG was named Outstanding Student Organization for the College of Science,
                recognising org performance, member programs, and community engagement across
                the year.

                The citation pointed at consistency rather than any single event: tutoring
                sessions that ran every week instead of only before finals, and a committee
                structure that kept working when the officers running it graduated.

                Awards like this are given to the year, not the ExeCom. Everyone who showed
                up to run a session, staff a booth, or answer a question in the group chat is
                part of why it landed.
                """,
        },
    ];
}
