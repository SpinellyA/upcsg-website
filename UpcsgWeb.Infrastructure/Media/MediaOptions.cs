namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Bound from the "Media" configuration section. Every value is a secret or a deployment
/// detail, so nothing here has a default that would work by accident — an unconfigured
/// app falls back to local disk rather than half-talking to a bucket.
/// </summary>
public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>Cloudflare account id; forms the S3 endpoint.</summary>
    public string? AccountId { get; set; }

    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }

    /// <summary>Bucket for images anyone may see: merch, events, people, achievements.</summary>
    public string? PublicBucket { get; set; }

    /// <summary>
    /// Origin the public bucket is served from — a custom domain like https://img.example.org,
    /// or the r2.dev URL for testing.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>How long an upload permission stays valid. Short: it is used immediately.</summary>
    public int UploadUrlMinutes { get; set; } = 10;

    /// <summary>Rejected above this, after upload, by reading the object back.</summary>
    public int MaxUploadBytes { get; set; } = 8 * 1024 * 1024;

    /// <summary>
    /// True only when everything needed to talk to R2 is present. Partial configuration is
    /// treated as unconfigured — a half-set bucket should fail loudly at startup, not
    /// mysteriously at the first upload.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(PublicBucket)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

    /// <summary>Which of the required keys are missing, for a startup diagnostic.</summary>
    public IReadOnlyList<string> MissingKeys()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(AccountId)) missing.Add($"{SectionName}:{nameof(AccountId)}");
        if (string.IsNullOrWhiteSpace(AccessKeyId)) missing.Add($"{SectionName}:{nameof(AccessKeyId)}");
        if (string.IsNullOrWhiteSpace(SecretAccessKey)) missing.Add($"{SectionName}:{nameof(SecretAccessKey)}");
        if (string.IsNullOrWhiteSpace(PublicBucket)) missing.Add($"{SectionName}:{nameof(PublicBucket)}");
        if (string.IsNullOrWhiteSpace(PublicBaseUrl)) missing.Add($"{SectionName}:{nameof(PublicBaseUrl)}");

        return missing;
    }
}
