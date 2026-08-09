using System.Text.RegularExpressions;

namespace UpcsgWeb.Domain.Media;

public static partial class MediaKeys
{
    private static readonly HashSet<string> AllowedFolders =
        new(StringComparer.OrdinalIgnoreCase) { "merch", "events", "members", "achievements", ReceiptsFolder };

    public const string ReceiptsFolder = "receipts";

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public static bool IsAllowedFolder(string folder) => AllowedFolders.Contains(folder);

    public static bool IsMemberWritableFolder(string folder) =>
        string.Equals(folder, ReceiptsFolder, StringComparison.OrdinalIgnoreCase);

    public static bool IsReceiptKey(string key) =>
        key.StartsWith(ReceiptsFolder + "/", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedType(string contentType) =>
        AllowedTypes.ContainsKey(contentType?.Split(';')[0].Trim() ?? string.Empty);

    public static IReadOnlyCollection<string> AllowedContentTypes => AllowedTypes.Keys;

    public static string Build(string folder, string fileName, string contentType)
    {
        var extension = AllowedTypes.TryGetValue(contentType.Split(';')[0].Trim(), out var ext)
            ? ext
            : Path.GetExtension(fileName);

        var stem = Slug(Path.GetFileNameWithoutExtension(fileName));
        var unique = Guid.NewGuid().ToString("n")[..12];

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
