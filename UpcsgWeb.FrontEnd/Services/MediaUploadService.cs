using System.Net.Http.Json;
using Microsoft.JSInterop;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMediaUploadService
{
    Task<UploadResult?> PickAndUploadAsync(string folder);
}

public sealed record UploadResult(string? Url, string? Error, long SizeBytes = 0, long OriginalBytes = 0);

public class MediaUploadService(HttpClient http, IJSRuntime js, ISessionStore session, ApiOptions options)
    : IMediaUploadService
{
    private const int MaxEdge = 1600;

    private const double Quality = 0.82;

    public async Task<UploadResult?> PickAndUploadAsync(string folder)
    {
        if (!options.IsConfigured)
        {
            return new UploadResult(null, "No API is configured, so there is nowhere to upload to.");
        }

        var picked = await js.InvokeAsync<PickedFile?>("upcsgPickImage", MaxEdge, Quality);

        if (picked is null)
        {
            return null;
        }

        var grantResponse = await http.PostAsJsonAsync("api/media/upload-url", new UploadGrantRequest
        {
            Folder = folder,
            FileName = picked.FileName,
            ContentType = picked.ContentType,
        }, UpcsgJson.Options);

        if (!grantResponse.IsSuccessStatusCode)
        {
            return new UploadResult(null, await DescribeAsync(grantResponse));
        }

        var grant = await grantResponse.Content.ReadFromJsonAsync<UploadGrantDto>(UpcsgJson.Options);

        if (grant is null)
        {
            return new UploadResult(null, "The server didn't return an upload URL.");
        }

        var token = grant.Method == "POST" ? (await session.ReadAsync())?.Token : null;

        var uploadError = await js.InvokeAsync<string>(
            "upcsgUpload", grant.UploadUrl, grant.Method, picked.ContentType, token);

        if (!string.IsNullOrEmpty(uploadError))
        {
            return new UploadResult(null, uploadError);
        }

        var confirmResponse = await http.PostAsJsonAsync("api/media/confirm",
            new ConfirmUploadRequest { Key = grant.Key }, UpcsgJson.Options);

        if (!confirmResponse.IsSuccessStatusCode)
        {
            return new UploadResult(null, await DescribeAsync(confirmResponse));
        }

        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<ConfirmUploadDto>(UpcsgJson.Options);

        return new UploadResult(
            confirmed?.StoredReference, null, confirmed?.SizeBytes ?? 0, picked.OriginalSize);
    }

    private static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body) ? $"Upload rejected ({(int)response.StatusCode})." : body;
    }

    private sealed record PickedFile(string FileName, string ContentType, long Size, long OriginalSize);
}
