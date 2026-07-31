using System.Text.RegularExpressions;

namespace UpcsgWeb.Infrastructure.Media;

/// <summary>
/// Builds storage keys. Shared by both stores so a key means the same thing on disk and
/// in the bucket, and switching provider doesn't invalidate what's already recorded.
/// </summary>
public static partial class MediaKeys
{
    private static readonly HashSet<string> AllowedFolders =
        new(StringComparer.OrdinalIgnoreCase) { "merch", "events", "members", "achievements", ReceiptsFolder };

    /// <summary>
    /// The one folder an ordinary guilder may write to. Everything else is site content
    /// and belongs to the officers; a receipt is the guilder's own proof of payment, so
    /// they have to be able to put it somewhere.
    /// </summary>
    public const string ReceiptsFolder = "receipts";

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public static bool IsAllowedFolder(string folder) => AllowedFolders.Contains(folder);

    /// <summary>True for folders any signed-in member may upload into.</summary>
    public static bool IsMemberWritableFolder(string folder) =>
        string.Equals(folder, ReceiptsFolder, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the key was issued for the receipts folder.</summary>
    public static bool IsReceiptKey(string key) =>
        key.StartsWith(ReceiptsFolder + "/", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedType(string contentType) =>
        AllowedTypes.ContainsKey(contentType?.Split(';')[0].Trim() ?? string.Empty);

    public static IReadOnlyCollection<string> AllowedContentTypes => AllowedTypes.Keys;

    /// <summary>
    /// A random key, not the uploaded filename.
    ///
    /// Two reasons: an attacker cannot enumerate the bucket by guessing names, and two
    /// officers uploading "hoodie.jpg" don't overwrite each other. The original name is
    /// kept only as a readable suffix.
    /// </summary>
    public static string Build(string folder, string fileName, string contentType)
    {
        var extension = AllowedTypes.TryGetValue(contentType.Split(';')[0].Trim(), out var ext)
            ? ext
            : Path.GetExtension(fileName);

        var stem = Slug(Path.GetFileNameWithoutExtension(fileName));
        var unique = Guid.NewGuid().ToString("n")[..12];

        // Date prefix keeps the bucket browsable and makes retention rules easy to write.
        return $"{folder.ToLowerInvariant()}/{DateTime.UtcNow:yyyy/MM}/{unique}{(stem.Length > 0 ? "-" + stem : string.Empty)}{extension}";
    }

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var slug = NonSlugChars().Replace(value.ToLowerInvariant(), "-").Trim('-');
        return slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();
}
