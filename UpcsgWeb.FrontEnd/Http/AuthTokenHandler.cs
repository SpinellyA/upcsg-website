using System.Net.Http.Headers;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.FrontEnd.Http;

/// <summary>
/// Attaches the stored API token to every outgoing request, so no endpoint client can
/// forget it. The token is read fresh each time, so signing out takes effect immediately
/// rather than leaving a stale header on a cached client.
///
/// Depends on ISessionStore, NOT IAuthService: AuthService needs HttpClient, and this
/// handler *is* part of HttpClient's construction, so depending on it would create a
/// resolution cycle that hangs the app at startup.
/// </summary>
public class AuthTokenHandler(ISessionStore store) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = await store.ReadAsync();

        if (session is not null && !string.IsNullOrWhiteSpace(session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
