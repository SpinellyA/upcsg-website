using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Any S3-compatible bucket: Supabase Storage, Cloudflare R2, MinIO, AWS.
///
/// This was R2MediaStore, with the Cloudflare endpoint built from an account id. The
/// protocol is identical across providers, so the vendor is now purely configuration —
/// changing providers is an environment-variable edit, not a code deployment.
///
/// Uploads are presigned so the browser PUTs straight to the bucket; bytes never touch
/// the API, which matters on a free-tier host that sleeps and has little headroom.
/// </summary>
public sealed class S3MediaStore : IMediaStore, IDisposable
{
    private readonly MediaOptions _options;
    private readonly AmazonS3Client _client;

    public S3MediaStore(IOptions<MediaOptions> options)
    {
        _options = options.Value;

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,

                // Neither Supabase nor R2 serves buckets as subdomains of the endpoint, so
                // path style is required; virtual-host addressing would build URLs that
                // resolve to nothing.
                AuthenticationRegion = _options.Region,
                ForcePathStyle = true,
            });
    }

    public string ProviderName => _options.DescribeProvider();

    /// <summary>
    /// Which bucket a key belongs in. Receipts are the guilder's payment details and go to
    /// the private bucket; everything else is site content meant to be seen.
    ///
    /// Falls back to the public bucket when no private one is configured, so an
    /// environment that predates Media:PrivateBucket keeps working — see the warning
    /// DescribeProvider emits in that case.
    /// </summary>
    private string BucketFor(string key) =>
        MediaKeys.IsReceiptKey(key) && _options.HasPrivateBucket
            ? _options.PrivateBucket!
            : _options.PublicBucket!;

    public bool IsPrivate(string key) =>
        MediaKeys.IsReceiptKey(key) && _options.HasPrivateBucket;

    public Task<UploadGrant> CreateUploadGrantAsync(
        string folder, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = MediaKeys.Build(folder, fileName, contentType);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketFor(key),
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_options.UploadUrlMinutes),

            // Binding the content type into the signature means the URL can only be used
            // to upload what was asked for — a signed URL for a JPEG can't smuggle HTML.
            ContentType = contentType,
        };

        var url = _client.GetPreSignedURL(request);

        // A private object has no durable URL to hand back, so the grant carries the key.
        // Whatever is in this field is what the caller persists, and for a receipt that has
        // to be the key — a presigned URL saved on an order would be dead within the hour.
        var stored = IsPrivate(key) ? key : PublicUrl(key);

        return Task.FromResult(new UploadGrant(key, url, stored, "PUT"));
    }

    public async Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default)
    {
        try
        {
            // BucketFor, not PublicBucket: confirm-after-upload reads the object back, and
            // looking in the wrong bucket would report every receipt as never having
            // arrived.
            var meta = await _client.GetObjectMetadataAsync(BucketFor(key), key, ct);
            return new StoredObject(meta.ContentLength, meta.Headers.ContentType ?? string.Empty);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default) =>
        await _client.DeleteObjectAsync(BucketFor(key), key, ct);

    public string PublicUrl(string key) =>
        $"{_options.PublicBaseUrl!.TrimEnd('/')}/{key}";

    public Task<string> CreateReadUrlAsync(string keyOrUrl, CancellationToken ct = default)
    {
        // Receipts recorded before the private bucket existed hold a full public URL.
        // Presigning that string would produce nonsense, so it is passed straight through.
        if (keyOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || keyOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(keyOrUrl);
        }

        if (!IsPrivate(keyOrUrl))
        {
            return Task.FromResult(PublicUrl(keyOrUrl));
        }

        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = BucketFor(keyOrUrl),
            Key = keyOrUrl,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_options.ReadUrlMinutes),
        });

        return Task.FromResult(url);
    }

    public void Dispose() => _client.Dispose();
}
