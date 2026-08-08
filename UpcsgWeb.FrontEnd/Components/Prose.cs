namespace UpcsgWeb.FrontEnd.Components;

public static class Prose
{
    public static IReadOnlyList<string> Paragraphs(string? body) =>
        string.IsNullOrWhiteSpace(body)
            ? []
            : [.. body
                .Replace("\r\n", "\n")
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Replace('\n', ' '))];

    public static string Standfirst(string? body) =>
        Paragraphs(body).FirstOrDefault() ?? string.Empty;

    public static IEnumerable<string> Rest(string? body) => Paragraphs(body).Skip(1);
}
