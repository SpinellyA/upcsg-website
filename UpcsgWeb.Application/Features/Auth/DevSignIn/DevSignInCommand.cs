using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.DevSignIn;

public record DevSignInCommand(string? Role, string? Email) : ICommand<AuthResultDto>;
