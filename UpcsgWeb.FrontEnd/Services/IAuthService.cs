using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IAuthService
{
    Task<AuthResultDto?> GetSessionAsync();

    Task<AuthResultDto> SignInWithGoogleAsync(string googleCredential);

    Task<AuthResultDto> SignInAsStubAsync(string role);

    Task SignOutAsync();
}
