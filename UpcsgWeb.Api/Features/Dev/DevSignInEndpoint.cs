using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Dev;

/// <summary>
/// Issues a genuine API token for local development, so the cart and CMS can be used
/// before Google sign-in is wired up.
///
/// DANGER: this hands out admin tokens to anyone who can reach it. Two independent
/// guards keep it out of production:
///   1. It implements IDevelopmentOnlyEndpoint, which Program.cs filters out of the
///      endpoint registry unless the host is Development, so the route does not exist.
///   2. The runtime check below, in case that filter is ever changed or bypassed.
///
/// Delete this folder once Google:ClientId is configured — it has no reason to outlive
/// the stub sign-in it exists to support.
/// </summary>
public class DevSignInEndpoint(
    IUserRepository users,
    IUnitOfWork uow,
    JwtIssuer jwt,
    IWebHostEnvironment environment,
    ILogger<DevSignInEndpoint> logger)
    : Endpoint<DevSignInRequest, AuthResultDto>, IDevelopmentOnlyEndpoint
{
    public override void Configure()
    {
        Post("/dev/signin");
        AllowAnonymous();
        Summary(s => s.Summary = "DEVELOPMENT ONLY. Issues a real token for a stub user.");
    }

    public override async Task HandleAsync(DevSignInRequest req, CancellationToken ct)
    {
        // Second guard. Belt and braces on purpose: the cost of this endpoint being
        // reachable in production is a total authorisation bypass.
        if (!environment.IsDevelopment())
        {
            logger.LogError("Dev sign-in was reached outside Development. Refusing.");
            await Send.NotFoundAsync(ct);
            return;
        }

        var wantsAdmin = string.Equals(req.Role, GuildRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var email = string.IsNullOrWhiteSpace(req.Email)
            ? (wantsAdmin ? "officer@up.edu.ph" : "guilder@up.edu.ph")
            : req.Email.Trim();

        // Keyed by a fake Google subject so repeated dev sign-ins reuse one row rather
        // than growing a pile of duplicate users.
        var subject = $"dev|{email}";

        var user = await users.GetByGoogleSubjectAsync(subject, ct);
        if (user is null)
        {
            user = AppUser.Create(subject, email, wantsAdmin ? "Dev Officer" : "Dev Guilder", null);
            users.Add(user);
        }
        else
        {
            user.RefreshProfile(email, user.Name, user.PictureUrl);
        }

        // The one place a role is assigned without a human deciding — which is precisely
        // why this endpoint cannot exist in production.
        if (wantsAdmin)
        {
            user.GrantAdmin();
        }
        else
        {
            user.RevokeAdmin();
        }

        await uow.SaveChangesAsync(ct);

        var (token, expiresAt) = jwt.Issue(user.Id, user.Email, user.Name, user.Role, user.PictureUrl);

        logger.LogWarning("Issued a DEVELOPMENT token for {Email} as {Role}.", user.Email, user.Role);

        await Send.OkAsync(new AuthResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToDto(),
        }, ct);
    }
}
