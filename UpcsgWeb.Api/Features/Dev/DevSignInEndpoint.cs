using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Auth.DevSignIn;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Dev;

public class DevSignInEndpoint(
    ISender sender,
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

        await Send.OkAsync(await sender.Send(new DevSignInCommand(req.Role, req.Email), ct), ct);
    }
}
