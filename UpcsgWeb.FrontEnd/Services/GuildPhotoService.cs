using System.Net.Http.Json;

namespace UpcsgWeb.FrontEnd.Services;

public class GuildPhoto
{
    public string File { get; set; } = string.Empty;

    public string? Caption { get; set; }
}

public interface IGuildPhotoService
{
    Task<List<GuildPhoto>> GetAsync();
}

// Photos are files dropped into wwwroot/img/guild and listed in photos.json. A Blazor
// WebAssembly app cannot enumerate a directory - there is no server to ask - so the
// manifest is what tells the page a file exists. Anything missing, malformed or empty
// yields no photos rather than an error: the About page is built to read without them.
public class GuildPhotoService(HttpClient http, ILogger<GuildPhotoService> logger)
    : IGuildPhotoService
{
    private const string ManifestPath = "img/guild/photos.json";
    private const string Folder = "img/guild/";

    private Task<List<GuildPhoto>>? _load;

    public Task<List<GuildPhoto>> GetAsync() => _load ??= LoadAsync();

    private async Task<List<GuildPhoto>> LoadAsync()
    {
        try
        {
            var manifest = await http.GetFromJsonAsync<List<GuildPhoto>>(
                ManifestPath, UpcsgJson.Options);

            if (manifest is null)
            {
                return [];
            }

            var usable = manifest
                .Where(p => !string.IsNullOrWhiteSpace(p.File))
                .Select(p => new GuildPhoto
                {
                    File = Folder + p.File.Trim(),
                    Caption = string.IsNullOrWhiteSpace(p.Caption) ? null : p.Caption.Trim(),
                })
                .ToList();

            logger.LogInformation("Loaded {Count} guild photos.", usable.Count);

            return usable;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No usable guild photo manifest at {Path}.", ManifestPath);
            return [];
        }
    }
}
