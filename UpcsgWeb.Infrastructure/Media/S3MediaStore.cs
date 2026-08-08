using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Infrastructure.Media;

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

                AuthenticationRegion = _options.Region,
                ForcePathStyle = true,
            });
    }

    public string ProviderName => _options.DescribeProvider();

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

            ContentType = contentType,
        };

        var url = _client.GetPreSignedURL(request);

        var stored = IsPrivate(key) ? key : PublicUrl(key);

        return Task.FromResult(new UploadGrant(key, url, stored, "PUT"));
    }

    public async Task<StoredObject?> InspectAsync(string key, CancellationToken ct = default)
    {
        try
        {
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
