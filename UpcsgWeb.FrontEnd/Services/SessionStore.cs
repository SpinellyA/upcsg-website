using System.Text.Json;
using Microsoft.JSInterop;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

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
