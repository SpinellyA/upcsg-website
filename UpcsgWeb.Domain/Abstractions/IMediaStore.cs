namespace UpcsgWeb.Domain.Abstractions;

public interface IMediaStore
{
    string ProviderName { get; }

    Task<UploadGrant> CreateUploadGrantAsync(
        string folder, string fileName, string contentType, CancellationToken ct = default);

    Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    string PublicUrl(string key);

    bool IsPrivate(string key);

    Task<string> CreateReadUrlAsync(string keyOrUrl, CancellationToken ct = default);
}

public sealed record UploadGrant(string Key, string UploadUrl, string PublicUrl, string Method);

public sealed record StoredObject(long SizeBytes, string ContentType);
