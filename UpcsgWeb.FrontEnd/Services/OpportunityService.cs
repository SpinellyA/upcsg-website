using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IOpportunityService
{
    Task<List<OpportunityDto>> GetOpenAsync();

    Task<OpportunityDto?> GetAsync(Guid id);
}

public class OpportunityService(HttpClient http, ApiOptions options, ISnapshotService snapshots)
    : IOpportunityService
{
    public Task<List<OpportunityDto>> GetOpenAsync() =>
        LiveOrSnapshot.ReadAsync(
            options,
            snapshots,
            async () => await http.GetFromJsonAsync<List<OpportunityDto>>(
                "api/opportunities", UpcsgJson.Options) ?? [],
            snapshot => snapshot.Opportunities.Where(o => !o.IsClosed).ToList(),
            () => []);

    public async Task<OpportunityDto?> GetAsync(Guid id)
    {
        if (!options.IsConfigured)
        {
            return (await GetOpenAsync()).FirstOrDefault(o => o.Id == id);
        }

        try
        {
            var response = await http.GetAsync($"api/opportunities/{id}");

            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<OpportunityDto>(UpcsgJson.Options)
                : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
