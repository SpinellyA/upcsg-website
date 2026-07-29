using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Cloudflare R2 over the S3 API.
///
/// Uploads are presigned so the browser PUTs straight to the bucket — bytes never touch
/// the API, which matters on a free-tier host that sleeps and has little headroom.
/// </summary>
public sealed class R2MediaStore : IMediaStore, IDisposable
{
    private readonly MediaOptions _options;
    private readonly AmazonS3Client _client;

    public R2MediaStore(IOptions<MediaOptions> options)
    {
        _options = options.Value;

        _client = new AmazonS3Client(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",

                // R2 has no regions, but the SDK signs with one; "auto" is what Cloudflare
                // expects. Path style because R2 does not do virtual-host buckets.
                AuthenticationRegion = "auto",
                ForcePathStyle = true,
            });
    }

    public string ProviderName => "Cloudflare R2";

    public Task<UploadGrant> CreateUploadGrantAsync(
        string folder, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = MediaKeys.Build(folder, fileName, contentType);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.PublicBucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_options.UploadUrlMinutes),

            // Binding the content type into the signature means the URL can only be used
            // to upload what was asked for — a signed URL for a JPEG can't smuggle HTML.
            ContentType = contentType,
        };

        var url = _client.GetPreSignedURL(request);

        return Task.FromResult(new UploadGrant(key, url, PublicUrl(key), "PUT"));
    }

    public async Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var meta = await _client.GetObjectMetadataAsync(_options.PublicBucket, key, ct);
            return new StoredObject(meta.ContentLength, meta.Headers.ContentType ?? string.Empty);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default) =>
        await _client.DeleteObjectAsync(_options.PublicBucket, key, ct);

    public string PublicUrl(string key) =>
        $"{_options.PublicBaseUrl!.TrimEnd('/')}/{key}";

    public void Dispose() => _client.Dispose();
}
