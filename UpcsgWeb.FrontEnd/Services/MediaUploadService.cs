using System.Net.Http.Json;
using Microsoft.JSInterop;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMediaUploadService
{
    /// <summary>
    /// Opens a file picker, downscales the chosen image, uploads it, and returns the URL
    /// to store. Null when the picker was closed without choosing anything.
    ///
    /// Officers use this for site content; guilders use it for the "receipts" folder,
    /// which is the only one the API lets an ordinary member write to.
    /// </summary>
    Task<UploadResult?> PickAndUploadAsync(string folder);
}

/// <param name="Url">Public URL to save on the entity.</param>
/// <param name="Error">Null on success; a message to show otherwise.</param>
public sealed record UploadResult(string? Url, string? Error, long SizeBytes = 0, long OriginalBytes = 0);

/// <summary>
/// Three steps, none of which carry the bytes through our own API when a bucket is
/// configured: ask for permission, PUT straight to storage, then have the server read the
/// object back and tell us it is acceptable.
/// </summary>
public class MediaUploadService(HttpClient http, IJSRuntime js, ISessionStore session, ApiOptions options)
    : IMediaUploadService
{
    /// <summary>
    /// Long edge cap. Comfortably above the largest slot the site renders (a 1:1 gallery
    /// at roughly 700 CSS px, doubled for retina), so nothing looks soft.
    /// </summary>
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

        // Only the local dev receiver needs our bearer token; a presigned bucket URL
        // carries its own signature and an Authorization header would invalidate it.
        var token = grant.Method == "POST" ? (await session.ReadAsync())?.Token : null;

        var uploadError = await js.InvokeAsync<string>(
            "upcsgUpload", grant.UploadUrl, grant.Method, picked.ContentType, token);

        if (!string.IsNullOrEmpty(uploadError))
        {
            return new UploadResult(null, uploadError);
        }

        // The signature binds the content type but nothing binds the size, so the server
        // reads the object back before we record the URL anywhere.
        var confirmResponse = await http.PostAsJsonAsync("api/media/confirm",
            new ConfirmUploadRequest { Key = grant.Key }, UpcsgJson.Options);

        if (!confirmResponse.IsSuccessStatusCode)
        {
            return new UploadResult(null, await DescribeAsync(confirmResponse));
        }

        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<ConfirmUploadDto>(UpcsgJson.Options);

        // StoredReference, not PublicUrl: for a receipt in the private bucket the former is
        // the storage key and the latter is deliberately empty. Saving PublicUrl here would
        // record an empty screenshot on the order and the domain would reject the receipt.
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
