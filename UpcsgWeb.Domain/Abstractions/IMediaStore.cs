namespace UpcsgWeb.Domain.Abstractions;

/// <summary>
/// Where uploaded images live.
///
/// A port alongside the repositories rather than a concrete client, so the API never
/// names a storage vendor. The local-disk implementation keeps development working with
/// no credentials at all; R2 takes over purely from configuration.
/// </summary>
public interface IMediaStore
{
    /// <summary>True when a real bucket is configured, false when falling back to disk.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Grants the browser permission to upload one object directly, so the bytes never
    /// pass through the API. On a free-tier host that is the difference between an upload
    /// working and timing out.
    /// </summary>
    Task<UploadGrant> CreateUploadGrantAsync(
        string folder, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// What the object actually turned out to be, read back from storage. The browser's
    /// declared content type is a claim, not evidence — this is how the claim gets checked.
    /// Null when no object exists at the key.
    /// </summary>
    Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>The URL a browser should use to read a public object.</summary>
    string PublicUrl(string key);

    /// <summary>
    /// True when this key lives somewhere the world cannot read. What the database records
    /// for such an object is a key, not a URL, so every view of it has to be minted.
    /// </summary>
    bool IsPrivate(string key);

    /// <summary>
    /// A URL that will read this object, valid only briefly.
    ///
    /// For a public object this is just <see cref="PublicUrl"/>. For a private one it is a
    /// presigned GET that expires, which is the point: a receipt link that leaks after the
    /// fact is worthless, unlike a permanent public URL.
    ///
    /// Accepts a value that is already an absolute URL and returns it unchanged, so
    /// receipts recorded before the private bucket existed keep rendering.
    /// </summary>
    Task<string> CreateReadUrlAsync(string keyOrUrl, CancellationToken ct = default);
}

/// <param name="Key">Storage key; what the database records.</param>
/// <param name="UploadUrl">Where the browser PUTs the bytes.</param>
/// <param name="PublicUrl">Where the image will be readable once uploaded.</param>
/// <param name="Method">PUT for a signed URL, POST for the local dev fallback.</param>
public sealed record UploadGrant(string Key, string UploadUrl, string PublicUrl, string Method);

public sealed record StoredObject(long SizeBytes, string ContentType);
