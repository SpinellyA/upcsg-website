using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Auth;

/// <summary>
/// Trades a Google ID token for one of ours, registering the guilder on first sign-in.
/// </summary>
public class GoogleSignInEndpoint(
    IUserRepository users,
    IUnitOfWork uow,
    IGoogleTokenVerifier verifier,
    JwtIssuer jwt,
    IConfiguration configuration,
    ILogger<GoogleSignInEndpoint> logger)
    : Endpoint<GoogleTokenExchangeRequest, AuthResultDto>
{
    public override void Configure()
    {
        Post("/auth/google");
        AllowAnonymous();
        Summary(s => s.Summary = "Exchange a Google ID token for a UPCSG API token.");
    }

    public override async Task HandleAsync(GoogleTokenExchangeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Credential))
        {
            AddError("A Google credential is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var identity = await verifier.VerifyAsync(req.Credential, ct);
        if (identity is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Optional tenant lock. Unset, any Google account may register.
        var requiredDomain = configuration["Google:RequiredHostedDomain"];
        if (!string.IsNullOrWhiteSpace(requiredDomain)
            && !string.Equals(identity.HostedDomain, requiredDomain, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Rejected sign-in from outside {Domain}: {Email}", requiredDomain, identity.Email);
            await Send.ForbiddenAsync(ct);
            return;
        }

        // Keyed on the Google subject, not email: a user can change their address, and
        // matching on email would eventually hand the account to whoever inherits it.
        var user = await users.GetByGoogleSubjectAsync(identity.Subject, ct);

        if (user is null)
        {
            user = AppUser.Create(identity.Subject, identity.Email, identity.Name, identity.PictureUrl);
            users.Add(user);
            logger.LogInformation("Registered guilder {Email}", identity.Email);
        }
        else
        {
            // Refreshes profile fields only — the aggregate gives no way to change Role
            // here, so a sign-in can never escalate privileges.
            user.RefreshProfile(identity.Email, identity.Name, identity.PictureUrl);
        }

        await uow.SaveChangesAsync(ct);

        var (token, expiresAt) = jwt.Issue(user.Id, user.Email, user.Name, user.Role, user.PictureUrl);

        await Send.OkAsync(new AuthResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToDto(),
        }, ct);
    }
}
