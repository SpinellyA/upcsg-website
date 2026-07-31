using FastEndpoints;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

/// <summary>
/// Receives the bytes when there is no bucket to presign against.
///
/// Only registered when the store really is the local one — with R2 configured this
/// endpoint does not exist, so there is no second, unsigned way in.
/// </summary>
public class LocalUploadEndpoint(IMediaStore media) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/media/local/{*key}");

        // Same split as the grant endpoint, or a guilder could be handed an upload URL
        // in dev and then be refused when the bytes actually arrive.
        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);

        // No AllowFileUploads(): the body is the raw image, not a multipart form, and that
        // call puts FastEndpoints into multipart mode where an image/jpeg body gets a 415.
        // Nothing needs binding here — the key is a route value and the body is a stream.
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (media is not LocalMediaStore local)
        {
            // Belt and braces: the endpoint is filtered out at startup, but if that ever
            // regressed this must not become an unsigned upload path into production.
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
            // Key resolved outside the media root.
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
