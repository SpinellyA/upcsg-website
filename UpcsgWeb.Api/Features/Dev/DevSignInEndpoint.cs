using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Dev;

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
