using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface ISnapshotService
{
    Task<ContentSnapshot?> GetAsync();

    bool IsServingSnapshot { get; }

    void NoteApiFailure();
}

public class SnapshotService(HttpClient http, ILogger<SnapshotService> logger) : ISnapshotService
{
    private const string SnapshotPath = "content-snapshot.json";

    private Task<ContentSnapshot?>? _load;

    public bool IsServingSnapshot { get; private set; }

    public void NoteApiFailure() => IsServingSnapshot = true;

    public Task<ContentSnapshot?> GetAsync() => _load ??= LoadAsync();

    private async Task<ContentSnapshot?> LoadAsync()
    {
        try
        {
            var snapshot = await http.GetFromJsonAsync<ContentSnapshot>(
                SnapshotPath, UpcsgJson.Options);

            if (snapshot is not null)
            {
                logger.LogInformation(
                    "Loaded content snapshot generated {GeneratedAt:u}.", snapshot.GeneratedAt);
            }

            return snapshot;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No usable content snapshot at {Path}.", SnapshotPath);
            return null;
        }
    }
}
