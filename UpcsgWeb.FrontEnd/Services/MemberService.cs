using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public class MemberService(HttpClient http, ApiOptions options, ISnapshotService snapshots)
    : IMemberService
{
    public async Task<MemberDto?> GetMemberAsync(Guid id)
    {
        if (!options.IsConfigured)
        {
            return (await GetMembersAsync()).FirstOrDefault(m => m.Id == id);
        }

        try
        {
            // 404 is an ordinary answer here — a bad id in the URL — so it must not
            // surface as an exception the page has to catch.
            var response = await http.GetAsync($"api/members/{id}");

            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<MemberDto>(UpcsgJson.Options)
                : null;
        }
        catch (HttpRequestException)
        {
            // The API is down rather than the id being wrong; the list path knows how to
            // fall back, so reuse it instead of returning a spurious "not found".
            return (await GetMembersAsync()).FirstOrDefault(m => m.Id == id);
        }
    }

    public Task<List<MemberDto>> GetMembersAsync() =>
        LiveOrSnapshot.ReadAsync(
            options,
            snapshots,
            async () => await http.GetFromJsonAsync<List<MemberDto>>("api/members", UpcsgJson.Options) ?? [],
            snapshot => snapshot.Members,
            SeedData);

    private static List<MemberDto> SeedData() =>
        [
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000001"),
                Name = "Dr. Juana Dela Cruz",
                Role = "Faculty Adviser",
                Category = MemberCategory.Faculty,
                Quote = "Teach the student, not the syllabus.",
                Bio = "Dr. Dela Cruz is an Associate Professor at the Department of Computer Science, "
                    + "UP Cebu. As faculty adviser she signs off on the guild's programs, mentors the "
                    + "executive committee through planning, and keeps the org's activities aligned "
                    + "with the department's academic goals.",
                DisplayOrder = 1,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000002"),
                Name = "Alex Santos",
                Role = "President",
                Category = MemberCategory.ExeCom,
                Committee = "Executive",
                Quote = "Ship it, then make it good.",
                Bio = "Alex, as President, oversees the direction, programs, and external standing of "
                    + "the guild. He presides over executive committee meetings, represents UPCSG to "
                    + "the college and to partner organizations, and is accountable to the membership "
                    + "for the org's plans each term.",
                DisplayOrder = 1,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000003"),
                Name = "Bea Reyes",
                Role = "Vice President - Internal",
                Category = MemberCategory.ExeCom,
                Committee = "Executive",
                Quote = "Nobody gets left behind on a group project.",
                Bio = "Bea handles everything that keeps the guild running inside: membership records, "
                    + "committee coordination, and internal events. She steps in for the President when "
                    + "needed and is usually the first person a new guilder talks to.",
                DisplayOrder = 2,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000004"),
                Name = "Carlo Mendoza",
                Role = "Vice President - External",
                Category = MemberCategory.ExeCom,
                Committee = "Executive",
                Quote = "Every cold email is a maybe.",
                Bio = "Carlo manages the guild's relationships outside UP Cebu. He secures sponsorships, "
                    + "coordinates with industry partners for talks and internships, and represents "
                    + "UPCSG in inter-university programmer networks.",
                DisplayOrder = 3,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000005"),
                Name = "Diane Uy",
                Role = "Secretary",
                Category = MemberCategory.ExeCom,
                Committee = "Executive",
                Quote = "If it isn't written down, it didn't happen.",
                Bio = "Diane keeps the guild's institutional memory. She records and circulates minutes, "
                    + "maintains the org's official documents and correspondence, and manages the "
                    + "calendar the rest of the committee works from.",
                DisplayOrder = 4,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000006"),
                Name = "Erik Villanueva",
                Role = "Treasurer",
                Category = MemberCategory.ExeCom,
                Committee = "Executive",
                Quote = "Budget twice, spend once.",
                Bio = "Erik is responsible for the guild's finances. He prepares and tracks the budget "
                    + "for every activity, collects and records membership dues and merch payments, and "
                    + "presents the financial report to the membership each semester.",
                DisplayOrder = 5,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000007"),
                Name = "Faith Ong",
                Role = "Committee Head",
                Category = MemberCategory.ExeCom,
                Committee = "Academics",
                Quote = "The best debugger is a friend who'll listen.",
                Bio = "Faith leads the Academics committee. She organizes tutorial sessions and review "
                    + "hubs before major exams, runs the guild's competitive programming training, and "
                    + "maintains the shared repository of course resources for guilders.",
                DisplayOrder = 6,
            },
            new MemberDto
            {
                Id = new Guid("00000000-0000-0000-0000-000000000008"),
                Name = "Gio Pascual",
                Role = "Committee Head",
                Category = MemberCategory.ExeCom,
                Committee = "Publicity & Creatives",
                Quote = "Make it legible before you make it pretty.",
                Bio = "Gio leads Publicity & Creatives. He directs the guild's visual identity across "
                    + "posters, socials, and merch, manages the org's online presence, and makes sure "
                    + "every activity actually reaches the students it's meant for.",
                DisplayOrder = 7,
            },
        ];
}
