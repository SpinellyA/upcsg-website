using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAdminContentService
{
    Task<List<MerchItemDto>> GetMerchAsync();
    Task<MerchItemDto> SaveMerchAsync(MerchItemDto item);
    Task DeleteMerchAsync(Guid id);

    Task<List<EventDto>> GetEventsAsync(int year, int month);
    Task<EventDto> SaveEventAsync(EventDto item);
    Task DeleteEventAsync(Guid id);

    Task<List<OpportunityDto>> GetOpportunitiesAsync();
    Task<OpportunityDto> SaveOpportunityAsync(OpportunityDto item);
    Task DeleteOpportunityAsync(Guid id);

    Task<List<MemberDto>> GetMembersAsync();
    Task<MemberDto> SaveMemberAsync(MemberDto item);
    Task DeleteMemberAsync(Guid id);

    Task<List<AchievementDto>> GetAchievementsAsync();
    Task<AchievementDto> SaveAchievementAsync(AchievementDto item);
    Task DeleteAchievementAsync(Guid id);

    Task<SiteSettingsDto> GetSettingsAsync();
    Task<SiteSettingsDto> SaveSettingsAsync(UpdateSiteSettingsRequest request);

    Task<List<OfficerEmailDto>> GetOfficersAsync();
    Task<OfficerEmailDto> AddOfficerAsync(AddOfficerRequest request);
    Task RemoveOfficerAsync(Guid id);

    Task<string> GetSnapshotJsonAsync();
}

public class AdminContentService(HttpClient http, ApiOptions options) : IAdminContentService
{

    public async Task<List<MerchItemDto>> GetMerchAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<MerchItemDto>>("api/admin/merch", UpcsgJson.Options) ?? [];
    }

    public Task<MerchItemDto> SaveMerchAsync(MerchItemDto item) =>
        SaveAsync(item, "api/merch", item.Id);

    public Task DeleteMerchAsync(Guid id) => DeleteAsync($"api/merch/{id}");

    public async Task<List<EventDto>> GetEventsAsync(int year, int month)
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<EventDto>>($"api/admin/events?year={year}&month={month}", UpcsgJson.Options) ?? [];
    }

    public Task<EventDto> SaveEventAsync(EventDto item) =>
        SaveAsync(item, "api/events", item.Id);

    public Task DeleteEventAsync(Guid id) => DeleteAsync($"api/events/{id}");

    public async Task<List<OpportunityDto>> GetOpportunitiesAsync() =>
        await http.GetFromJsonAsync<List<OpportunityDto>>("api/admin/opportunities", UpcsgJson.Options) ?? [];

    public Task<OpportunityDto> SaveOpportunityAsync(OpportunityDto item) =>
        SaveAsync(item, "api/opportunities", item.Id);

    public Task DeleteOpportunityAsync(Guid id) => DeleteAsync($"api/opportunities/{id}");

    public async Task<List<MemberDto>> GetMembersAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<MemberDto>>("api/members", UpcsgJson.Options) ?? [];
    }

    public Task<MemberDto> SaveMemberAsync(MemberDto item) =>
        SaveAsync(item, "api/members", item.Id);

    public Task DeleteMemberAsync(Guid id) => DeleteAsync($"api/members/{id}");

    public async Task<List<AchievementDto>> GetAchievementsAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<AchievementDto>>("api/achievements", UpcsgJson.Options) ?? [];
    }

    public Task<AchievementDto> SaveAchievementAsync(AchievementDto item) =>
        SaveAsync(item, "api/achievements", item.Id);

    public Task DeleteAchievementAsync(Guid id) => DeleteAsync($"api/achievements/{id}");

    public async Task<SiteSettingsDto> GetSettingsAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<SiteSettingsDto>("api/settings", UpcsgJson.Options) ?? new SiteSettingsDto();
    }

    public async Task<SiteSettingsDto> SaveSettingsAsync(UpdateSiteSettingsRequest request)
    {
        EnsureConfigured();

        var response = await http.PutAsJsonAsync("api/settings", request, UpcsgJson.Options);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<SiteSettingsDto>(UpcsgJson.Options) ?? new SiteSettingsDto();
    }

    public async Task<List<OfficerEmailDto>> GetOfficersAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<OfficerEmailDto>>("api/admin/officers", UpcsgJson.Options) ?? [];
    }

    public async Task<OfficerEmailDto> AddOfficerAsync(AddOfficerRequest request)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync("api/admin/officers", request, UpcsgJson.Options);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<OfficerEmailDto>(UpcsgJson.Options)
            ?? throw new ApiException("The API returned an empty response.");
    }

    public Task RemoveOfficerAsync(Guid id) => DeleteAsync($"api/admin/officers/{id}");

    public async Task<string> GetSnapshotJsonAsync()
    {
        EnsureConfigured();

        var response = await http.GetAsync("api/snapshot");
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<T> SaveAsync<T>(T payload, string collectionUrl, Guid id)
    {
        EnsureConfigured();

        var response = id == Guid.Empty
            ? await http.PostAsJsonAsync(collectionUrl, payload, UpcsgJson.Options)
            : await http.PutAsJsonAsync($"{collectionUrl}/{id}", payload, UpcsgJson.Options);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<T>(UpcsgJson.Options)
            ?? throw new ApiException("The API returned an empty response.");
    }

    private async Task DeleteAsync(string url)
    {
        EnsureConfigured();

        var response = await http.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }
    }

    private void EnsureConfigured()
    {
        if (!options.IsConfigured)
        {
            throw new ApiException("No API is configured. Set Api:BaseUrl in wwwroot/appsettings.json.");
        }
    }
}
