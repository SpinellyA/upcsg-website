using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Officer-side writes for site content. Grouped into one client rather than four,
/// because the CMS is a single screen family and this keeps the DI surface small.
/// </summary>
public interface IAdminContentService
{
    Task<List<MerchItemDto>> GetMerchAsync();
    Task<MerchItemDto> SaveMerchAsync(MerchItemDto item);
    Task DeleteMerchAsync(int id);

    Task<List<EventDto>> GetEventsAsync(int year, int month);
    Task<EventDto> SaveEventAsync(EventDto item);
    Task DeleteEventAsync(int id);

    Task<List<MemberDto>> GetMembersAsync();
    Task<MemberDto> SaveMemberAsync(MemberDto item);
    Task DeleteMemberAsync(int id);

    Task<List<AchievementDto>> GetAchievementsAsync();
    Task<AchievementDto> SaveAchievementAsync(AchievementDto item);
    Task DeleteAchievementAsync(int id);

    Task<SiteSettingsDto> GetSettingsAsync();
    Task<SiteSettingsDto> SaveSettingsAsync(UpdateSiteSettingsRequest request);
}

public class AdminContentService(HttpClient http, ApiOptions options) : IAdminContentService
{
    // --- Merch ----------------------------------------------------------------------

    public async Task<List<MerchItemDto>> GetMerchAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<MerchItemDto>>("api/admin/merch", UpcsgJson.Options) ?? [];
    }

    public Task<MerchItemDto> SaveMerchAsync(MerchItemDto item) =>
        SaveAsync(item, "api/merch", item.Id);

    public Task DeleteMerchAsync(int id) => DeleteAsync($"api/merch/{id}");

    // --- Events ---------------------------------------------------------------------

    public async Task<List<EventDto>> GetEventsAsync(int year, int month)
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<EventDto>>($"api/admin/events?year={year}&month={month}", UpcsgJson.Options) ?? [];
    }

    public Task<EventDto> SaveEventAsync(EventDto item) =>
        SaveAsync(item, "api/events", item.Id);

    public Task DeleteEventAsync(int id) => DeleteAsync($"api/events/{id}");

    // --- Members --------------------------------------------------------------------

    public async Task<List<MemberDto>> GetMembersAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<MemberDto>>("api/members", UpcsgJson.Options) ?? [];
    }

    public Task<MemberDto> SaveMemberAsync(MemberDto item) =>
        SaveAsync(item, "api/members", item.Id);

    public Task DeleteMemberAsync(int id) => DeleteAsync($"api/members/{id}");

    // --- Achievements ---------------------------------------------------------------

    public async Task<List<AchievementDto>> GetAchievementsAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<AchievementDto>>("api/achievements", UpcsgJson.Options) ?? [];
    }

    public Task<AchievementDto> SaveAchievementAsync(AchievementDto item) =>
        SaveAsync(item, "api/achievements", item.Id);

    public Task DeleteAchievementAsync(int id) => DeleteAsync($"api/achievements/{id}");

    // --- Settings -------------------------------------------------------------------

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

    // --- Shared -------------------------------------------------------------------

    /// <summary>Id of zero means create; anything else updates in place.</summary>
    private async Task<T> SaveAsync<T>(T payload, string collectionUrl, int id)
    {
        EnsureConfigured();

        var response = id == 0
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
