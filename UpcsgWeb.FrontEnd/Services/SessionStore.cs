using System.Text.Json;
using Microsoft.JSInterop;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Reads and writes the stored session. Depends on JS interop only.
///
/// This exists to break a dependency cycle: AuthTokenHandler needs the token, but if it
/// asked IAuthService for it, the graph became
///   HttpClient -> AuthTokenHandler -> IAuthService -> AuthService(HttpClient) -> HttpClient
/// and because that cycle runs through a factory delegate, DI cannot detect it — it just
/// recurses until the WebAssembly stack blows, hanging the app at 100% with no error.
/// Anything the handler depends on must therefore stay clear of HttpClient.
/// </summary>
public interface ISessionStore
{
    Task<AuthResultDto?> ReadAsync();
    Task WriteAsync(AuthResultDto session);
    Task ClearAsync();
}

public class SessionStore(IJSRuntime js) : ISessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthResultDto?> ReadAsync()
    {
        var raw = await js.InvokeAsync<string?>("upcsgStorage.get", AuthConfig.SessionStorageKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        AuthResultDto? session;
        try
        {
            session = JsonSerializer.Deserialize<AuthResultDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            // Shape changed or the value was tampered with — drop it rather than trust it.
            await ClearAsync();
            return null;
        }

        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await ClearAsync();
            return null;
        }

        return session;
    }

    public async Task WriteAsync(AuthResultDto session) =>
        await js.InvokeVoidAsync("upcsgStorage.set",
            AuthConfig.SessionStorageKey,
            JsonSerializer.Serialize(session, JsonOptions));

    public async Task ClearAsync() =>
        await js.InvokeVoidAsync("upcsgStorage.remove", AuthConfig.SessionStorageKey);
}
