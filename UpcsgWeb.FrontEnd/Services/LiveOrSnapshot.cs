using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public static class LiveOrSnapshot
{
    public static async Task<T> ReadAsync<T>(
        ApiOptions options,
        ISnapshotService snapshots,
        Func<Task<T>> fromApi,
        Func<ContentSnapshot, T> fromSnapshot,
        Func<T> seed)
    {
        if (options.IsConfigured)
        {
            try
            {
                return await fromApi();
            }
            catch (Exception ex) when (IsUnreachable(ex))
            {
                snapshots.NoteApiFailure();
            }
        }

        var snapshot = await snapshots.GetAsync();

        return snapshot is null ? seed() : fromSnapshot(snapshot);
    }

    private static bool IsUnreachable(Exception ex) =>
        ex is HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or System.Text.Json.JsonException;
}
