using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Try the API; fall back to the committed snapshot when it cannot be reached.
///
/// One place rather than four, because the interesting part is which failures count as
/// "unreachable". A 404 for one merch item is an ordinary answer and must not tip the
/// whole site into offline mode — only a transport failure or a server error does.
/// </summary>
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

        // The built-in seed is the last resort: a brand-new deployment with no API and no
        // snapshot yet still renders something rather than a blank site.
        return snapshot is null ? seed() : fromSnapshot(snapshot);
    }

    /// <summary>
    /// A dead API, not a bad request.
    ///
    /// HttpRequestException covers DNS failure, connection refused, TLS problems and the
    /// 5xx responses that GetFromJsonAsync raises — which is exactly the sleeping-host
    /// case. TaskCanceledException is the timeout. A JSON parse failure counts too: a
    /// proxy or captive portal returning an HTML error page is the API being unusable,
    /// however cheerful the status code.
    /// </summary>
    private static bool IsUnreachable(Exception ex) =>
        ex is HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or System.Text.Json.JsonException;
}
