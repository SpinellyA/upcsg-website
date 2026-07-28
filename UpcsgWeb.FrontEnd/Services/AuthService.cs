using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Session lifecycle. Storage is delegated to ISessionStore so that AuthTokenHandler can
/// read the token without depending on this type — see the note in SessionStore.
/// </summary>
public class AuthService(HttpClient http, ISessionStore store, ApiOptions api) : IAuthService
{
    public Task<AuthResultDto?> GetSessionAsync() => store.ReadAsync();

    public async Task<AuthResultDto> SignInWithGoogleAsync(string googleCredential)
    {
        var response = await http.PostAsJsonAsync("api/auth/google",
            new GoogleTokenExchangeRequest { Credential = googleCredential },
            UpcsgJson.Options);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(UpcsgJson.Options)
            ?? throw new ApiException("The sign-in response was empty.");

        await store.WriteAsync(result);
        return result;
    }

    public async Task<AuthResultDto> SignInAsStubAsync(string role)
    {
        var isAdmin = string.Equals(role, UpcsgRoles.Admin, StringComparison.OrdinalIgnoreCase);

        // With an API configured, ask it for a genuine token. A locally fabricated one
        // authenticates the Blazor UI but is rejected by the API, which shows up as 401s
        // on every authenticated page — cart, orders, and the whole CMS.
        if (api.IsConfigured)
        {
            var response = await http.PostAsJsonAsync("api/dev/signin",
                new { Role = isAdmin ? UpcsgRoles.Admin : UpcsgRoles.Member },
                UpcsgJson.Options);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(
                    "Dev sign-in failed. It only exists when the API runs in Development — "
                    + $"{await CartService.DescribeAsync(response)}");
            }

            var issued = await response.Content.ReadFromJsonAsync<AuthResultDto>(UpcsgJson.Options)
                ?? throw new ApiException("Dev sign-in returned an empty response.");

            await store.WriteAsync(issued);
            return issued;
        }

        // No API: a local-only identity so the authenticated UI can still be exercised.
        var offline = new AuthResultDto
        {
            Token = "stub-token-not-a-real-jwt",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(8),
            User = new AppUserDto
            {
                Id = isAdmin ? "stub-admin" : "stub-member",
                Email = isAdmin ? "officer@up.edu.ph" : "guilder@up.edu.ph",
                Name = isAdmin ? "Stub Officer" : "Stub Guilder",
                Role = isAdmin ? UpcsgRoles.Admin : UpcsgRoles.Member,
            },
        };

        await store.WriteAsync(offline);
        return offline;
    }

    public Task SignOutAsync() => store.ClearAsync();
}
