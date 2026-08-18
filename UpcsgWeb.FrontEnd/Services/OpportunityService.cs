using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IOpportunityService
{
    /// <summary>
    /// Every opportunity, open and closed. Callers that only want live entries filter on
    /// <see cref="OpportunityDto.IsClosed"/>; the opportunities page shows both, in
    /// separate sections.
    /// </summary>
    Task<List<OpportunityDto>> GetAllAsync();

    Task<OpportunityDto?> GetAsync(Guid id);
}

public class OpportunityService(HttpClient http, ApiOptions options, ISnapshotService snapshots)
    : IOpportunityService
{
    public Task<List<OpportunityDto>> GetAllAsync() =>
        LiveOrSnapshot.ReadAsync(
            options,
            snapshots,
            async () => await http.GetFromJsonAsync<List<OpportunityDto>>(
                "api/opportunities", UpcsgJson.Options) ?? [],
            snapshot => snapshot.Opportunities,
            () => []);

    public async Task<OpportunityDto?> GetAsync(Guid id)
    {
        if (!options.IsConfigured)
        {
            return (await GetAllAsync()).FirstOrDefault(o => o.Id == id);
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
