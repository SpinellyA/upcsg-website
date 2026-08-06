namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Bound from the "Media" configuration section. Every value is a secret or a deployment
/// detail, so nothing here has a default that would work by accident — an unconfigured
/// app falls back to local disk rather than half-talking to a bucket.
///
/// Deliberately vendor-neutral. This used to carry a Cloudflare account id and build the
/// R2 endpoint from it, which made moving to another provider a code change rather than a
/// configuration one. Supabase Storage, R2, MinIO and AWS all speak the same protocol;
/// what differs is the endpoint and the region.
/// </summary>
public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>
    /// Full S3 endpoint of the provider.
    ///
    ///   Supabase   https://[project-ref].supabase.co/storage/v1/s3
    ///   R2         https://[account-id].r2.cloudflarestorage.com
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// The region the credentials are signed for.
    ///
    /// Supabase issues a per-project region and rejects a signature made for another one.
    /// R2 has no regions, but the SDK still signs with something and Cloudflare expects
    /// the literal "auto" — which is why this is configuration rather than a constant.
    /// </summary>
    public string Region { get; set; } = "auto";

    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }

    /// <summary>Bucket for images anyone may see: merch, events, people, achievements.</summary>
    public string? PublicBucket { get; set; }

    /// <summary>
    /// Bucket for objects that must not be world-readable — today, GCash receipts.
    ///
    /// Optional, and its absence is a deliberate fallback rather than an error: leaving it
    /// unset puts receipts back in the public bucket, which is what this deployment did
    /// before the private bucket existed. That keeps a half-configured environment working
    /// instead of breaking checkout, but it is the less safe of the two states — a receipt
    /// there is readable by anyone who has, or guesses, the URL.
    ///
    /// The startup log says which one is in force.
    /// </summary>
    public string? PrivateBucket { get; set; }

    /// <summary>True when receipts get a bucket of their own.</summary>
    public bool HasPrivateBucket => !string.IsNullOrWhiteSpace(PrivateBucket);

    /// <summary>
    /// Lifetime of a presigned read URL for a private object.
    ///
    /// Long enough for an officer to open the image and look at it, short enough that a
    /// URL copied out of the address bar and pasted somewhere is worthless by the time
    /// anyone else follows it.
    /// </summary>
    public int ReadUrlMinutes { get; set; } = 15;

    /// <summary>
    /// Origin the public bucket is served from.
    ///
    ///   Supabase   https://[project-ref].supabase.co/storage/v1/object/public/[bucket]
    ///   R2         a custom domain, or the r2.dev URL for testing
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>How long an upload permission stays valid. Short: it is used immediately.</summary>
    public int UploadUrlMinutes { get; set; } = 10;

    /// <summary>Rejected above this, after upload, by reading the object back.</summary>
    public int MaxUploadBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>
    /// True only when everything needed to talk to the bucket is present. Partial
    /// configuration is treated as unconfigured — a half-set bucket should be visible at
    /// startup, not mysteriously at the first upload.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(PublicBucket)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

    /// <summary>Which of the required keys are missing, for a startup diagnostic.</summary>
    public IReadOnlyList<string> MissingKeys()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(ServiceUrl)) missing.Add($"{SectionName}:{nameof(ServiceUrl)}");
        if (string.IsNullOrWhiteSpace(AccessKeyId)) missing.Add($"{SectionName}:{nameof(AccessKeyId)}");
        if (string.IsNullOrWhiteSpace(SecretAccessKey)) missing.Add($"{SectionName}:{nameof(SecretAccessKey)}");
        if (string.IsNullOrWhiteSpace(PublicBucket)) missing.Add($"{SectionName}:{nameof(PublicBucket)}");
        if (string.IsNullOrWhiteSpace(PublicBaseUrl)) missing.Add($"{SectionName}:{nameof(PublicBaseUrl)}");

        return missing;
    }

    /// <summary>Named for the startup log, so a deployment shows what it connected to.</summary>
    public string DescribeProvider()
    {
        if (string.IsNullOrWhiteSpace(ServiceUrl))
        {
            return "unconfigured";
        }

        var vendor = ServiceUrl.Contains("supabase", StringComparison.OrdinalIgnoreCase)
            ? "Supabase Storage"
            : ServiceUrl.Contains("r2.cloudflarestorage", StringComparison.OrdinalIgnoreCase)
                ? "Cloudflare R2"
                : "S3-compatible storage";

        // Named at startup because the unsafe state is the silent one: without a private
        // bucket, receipts still upload and still display, and nothing looks wrong.
        return HasPrivateBucket
            ? $"{vendor} (receipts in '{PrivateBucket}')"
            : $"{vendor} — NO PRIVATE BUCKET: receipts go to the public bucket and are "
              + "readable by anyone with the URL. Set Media:PrivateBucket.";
    }
}
