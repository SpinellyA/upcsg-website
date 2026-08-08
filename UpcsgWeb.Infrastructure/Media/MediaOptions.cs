namespace UpcsgWeb.Infrastructure.Media;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    public string? ServiceUrl { get; set; }

    public string Region { get; set; } = "auto";

    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }

    public string? PublicBucket { get; set; }

    public string? PrivateBucket { get; set; }

    public bool HasPrivateBucket => !string.IsNullOrWhiteSpace(PrivateBucket);

    public int ReadUrlMinutes { get; set; } = 15;

    public string? PublicBaseUrl { get; set; }

    public int UploadUrlMinutes { get; set; } = 10;

    public int MaxUploadBytes { get; set; } = 8 * 1024 * 1024;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServiceUrl)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(PublicBucket)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

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

        return HasPrivateBucket
            ? $"{vendor} (receipts in '{PrivateBucket}')"
            : $"{vendor} — NO PRIVATE BUCKET: receipts go to the public bucket and are "
              + "readable by anyone with the URL. Set Media:PrivateBucket.";
    }
}
