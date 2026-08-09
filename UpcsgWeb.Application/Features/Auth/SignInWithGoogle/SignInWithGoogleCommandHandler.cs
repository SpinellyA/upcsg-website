using Microsoft.Extensions.Logging;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Features.Officers;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.SignInWithGoogle;

public class SignInWithGoogleCommandHandler(
    IUnitOfWork uow,
    IGoogleTokenVerifier verifier,
    ITokenIssuer tokens,
    SignInOptions options,
    ILogger<SignInWithGoogleCommandHandler> logger)
    : ICommandHandler<SignInWithGoogleCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        SignInWithGoogleCommand command,
        CancellationToken cancellationToken)
    {
        var identity = await verifier.VerifyAsync(command.Credential, cancellationToken)
            ?? throw new UnauthorizedException("That Google sign-in could not be verified.");

        var requiredDomain = options.RequiredHostedDomain;

        if (!string.IsNullOrWhiteSpace(requiredDomain)
            && !string.Equals(identity.HostedDomain, requiredDomain, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Rejected sign-in from outside {Domain}: {Email}", requiredDomain, identity.Email);

            throw new ForbiddenException(
                $"Sign-in is limited to {requiredDomain} accounts.");
        }

        var user = await uow.Users.GetByGoogleSubjectAsync(identity.Subject, cancellationToken);

        if (user is null)
        {
            user = AppUser.Create(identity.Subject, identity.Email, identity.Name, identity.PictureUrl);
            uow.Users.Add(user);
            logger.LogInformation("Registered guilder {Email}", identity.Email);
        }
        else
        {
            user.RefreshProfile(identity.Email, identity.Name, identity.PictureUrl);
        }

        if (await SyncOfficerRole.ApplyAsync(uow, user, cancellationToken))
        {
            logger.LogInformation("{Email} signed in as {Role}.", user.Email, user.Role);
        }

        await uow.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = tokens.Issue(
            user.Id, user.Email, user.Name, user.Role, user.PictureUrl);

        return new AuthResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToDto(),
        };
    }
}
