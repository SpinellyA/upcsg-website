using FastEndpoints;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Media;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

public class LocalUploadEndpoint(IMediaStore media) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/media/local/{*key}");

        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);

    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (media is not LocalMediaStore local)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var key = Uri.UnescapeDataString(Route<string>("key") ?? string.Empty);

        if (string.IsNullOrWhiteSpace(key))
        {
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (!MediaKeys.IsReceiptKey(key) && !User.IsInRole(UpcsgRoles.Admin))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            await local.SaveAsync(key, HttpContext.Request.Body, ct);
        }
        catch (InvalidOperationException)
        {
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
