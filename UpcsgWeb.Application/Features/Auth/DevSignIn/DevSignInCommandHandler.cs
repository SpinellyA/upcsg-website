using Microsoft.Extensions.Logging;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.DevSignIn;

public class DevSignInCommandHandler(
    IUnitOfWork uow,
    ITokenIssuer tokens,
    ILogger<DevSignInCommandHandler> logger)
    : ICommandHandler<DevSignInCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        DevSignInCommand command,
        CancellationToken cancellationToken)
    {
        var wantsAdmin = string.Equals(command.Role, GuildRoles.Admin, StringComparison.OrdinalIgnoreCase);

        var email = string.IsNullOrWhiteSpace(command.Email)
            ? (wantsAdmin ? "officer@up.edu.ph" : "guilder@up.edu.ph")
            : command.Email.Trim();

        var subject = $"dev|{email}";

        var user = await uow.Users.GetByGoogleSubjectAsync(subject, cancellationToken);

        if (user is null)
        {
            user = AppUser.Create(subject, email, wantsAdmin ? "Dev Officer" : "Dev Guilder", null);
            uow.Users.Add(user);
        }
        else
        {
            user.RefreshProfile(email, user.Name, user.PictureUrl);
        }

        if (wantsAdmin)
        {
            user.GrantAdmin();
        }
        else
        {
            user.RevokeAdmin();
        }

        await uow.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = tokens.Issue(
            user.Id, user.Email, user.Name, user.Role, user.PictureUrl);

        logger.LogWarning("Issued a DEVELOPMENT token for {Email} as {Role}.", user.Email, user.Role);

        return new AuthResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToDto(),
        };
    }
}
