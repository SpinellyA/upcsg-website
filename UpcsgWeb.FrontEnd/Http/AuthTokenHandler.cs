using System.Net.Http.Headers;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.FrontEnd.Http;

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
