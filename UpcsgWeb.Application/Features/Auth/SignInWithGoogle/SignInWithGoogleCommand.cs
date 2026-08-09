using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.SignInWithGoogle;

public record SignInWithGoogleCommand(string Credential) : ICommand<AuthResultDto>;
