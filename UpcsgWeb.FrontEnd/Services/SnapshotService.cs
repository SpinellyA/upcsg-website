using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface ISnapshotService
{
    /// <summary>
    /// The committed snapshot, or null when there isn't one. Fetched once per visit.
    /// </summary>
    Task<ContentSnapshot?> GetAsync();

    /// <summary>True once a live call has failed and the site is running on the snapshot.</summary>
    bool IsServingSnapshot { get; }

    /// <summary>Records that a live call failed, so the banner can say so.</summary>
    void NoteApiFailure();
}

/// <summary>
/// Loads content-snapshot.json from the published site.
///
/// This is what the public pages fall back to when the API cannot be reached. It used to
/// be a hand-written seed list inside each service, which had two problems: the fallback
/// showed invented people and events rather than the guild's real ones, and it only
/// applied when no API was configured at all — a configured but sleeping API threw, and
/// the page died instead of degrading.
/// </summary>
public class SnapshotService(HttpClient http, ILogger<SnapshotService> logger) : ISnapshotService
{
    // The file lives beside the app, not behind the API — fetching it through the API
    // would defeat the entire point.
    private const string SnapshotPath = "content-snapshot.json";

    private Task<ContentSnapshot?>? _load;

    public bool IsServingSnapshot { get; private set; }

    public void NoteApiFailure() => IsServingSnapshot = true;

    public Task<ContentSnapshot?> GetAsync() => _load ??= LoadAsync();

    private async Task<ContentSnapshot?> LoadAsync()
    {
        try
        {
            // Relative to the app's base href, so this works from a GitHub Pages project
            // subpath as well as from a domain root.
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
            // No snapshot committed yet is a normal state for a new deployment, not an
            // error worth breaking the page over.
            logger.LogWarning(ex, "No usable content snapshot at {Path}.", SnapshotPath);
            return null;
        }
    }
}
