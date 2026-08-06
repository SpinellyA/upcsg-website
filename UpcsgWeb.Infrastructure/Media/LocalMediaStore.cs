using Microsoft.Extensions.Options;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Writes to the API's own wwwroot so uploads work with no cloud credentials at all.
///
/// For development only, and it says so at startup. Render's disk is ephemeral: anything
/// written here disappears on the next deploy or cold start, which is exactly why the
/// production path is a bucket.
///
/// There is no presigned URL to give out, so the grant points back at an API endpoint the
/// browser POSTs to instead. The shape of the flow stays identical either way, which is
/// what makes swapping providers a config change rather than a rewrite.
/// </summary>
public sealed class LocalMediaStore : IMediaStore
{
    private readonly string _root;
    private readonly string _baseUrl;

    public LocalMediaStore(IOptions<MediaOptions> options, string contentRoot, string baseUrl)
    {
        _root = Path.Combine(contentRoot, "wwwroot", "media");
        _baseUrl = baseUrl.TrimEnd('/');
        Directory.CreateDirectory(_root);
    }

    public string ProviderName => "local disk (development)";

    public Task<UploadGrant> CreateUploadGrantAsync(
        string folder, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = MediaKeys.Build(folder, fileName, contentType);

        return Task.FromResult(new UploadGrant(
            key,
            $"{_baseUrl}/api/media/local/{Uri.EscapeDataString(key)}",
            PublicUrl(key),
            "POST"));
    }

    public Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);

        if (!File.Exists(path))
        {
            return Task.FromResult<StoredObject?>(null);
        }

        var info = new FileInfo(path);
        return Task.FromResult<StoredObject?>(new StoredObject(info.Length, ContentTypeOf(key)));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public string PublicUrl(string key) => $"{_baseUrl}/media/{key}";

    /// <summary>
    /// Always false. This store writes into the API's own wwwroot, which is served by
    /// UseStaticFiles — there is nowhere here that is not world-readable, and claiming
    /// otherwise would let a development run look safer than it is.
    /// </summary>
    public bool IsPrivate(string key) => false;

    public Task<string> CreateReadUrlAsync(string keyOrUrl, CancellationToken ct = default) =>
        Task.FromResult(
            keyOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || keyOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? keyOrUrl
                : PublicUrl(keyOrUrl));

    /// <summary>Used by the local upload endpoint; not part of the port.</summary>
    public async Task SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    /// <summary>
    /// Resolves a key under the media root and refuses anything that escapes it. Keys are
    /// server-generated, but this endpoint takes one from the request, so "../.." has to
    /// be impossible rather than merely unlikely.
    /// </summary>
    private string PathFor(string key)
    {
        var combined = Path.GetFullPath(Path.Combine(_root, key));
        var root = Path.GetFullPath(_root);

        if (!combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Rejected a media key that resolves outside the media root.");
        }

        return combined;
    }

    private static string ContentTypeOf(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream",
    };
}
