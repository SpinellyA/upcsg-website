using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Auth.SignInWithGoogle;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Auth;

public class GoogleSignInEndpoint(ISender sender) : Endpoint<GoogleTokenExchangeRequest, AuthResultDto>
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

        await Send.OkAsync(await sender.Send(new SignInWithGoogleCommand(req.Credential), ct), ct);
    }
}
