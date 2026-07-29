namespace UpcsgWeb.FrontEnd.Components;

/// <summary>
/// CMS body copy is a single text column with blank lines between paragraphs. This turns
/// that into renderable paragraphs so long entries don't come out as one wall of text.
/// </summary>
public static class Prose
{
    /// <summary>Blank-line-separated paragraphs, with soft wraps inside one collapsed.</summary>
    public static IReadOnlyList<string> Paragraphs(string? body) =>
        string.IsNullOrWhiteSpace(body)
            ? []
            : [.. body
                .Replace("\r\n", "\n")
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Replace('\n', ' '))];

    /// <summary>
    /// The opening paragraph, used as the standfirst on cards. Falls back to the whole
    /// body when there are no blank lines, so a one-line entry still shows something.
    /// </summary>
    public static string Standfirst(string? body) =>
        Paragraphs(body).FirstOrDefault() ?? string.Empty;

    /// <summary>Everything after the standfirst — empty for a single-paragraph entry.</summary>
    public static IEnumerable<string> Rest(string? body) => Paragraphs(body).Skip(1);
}
