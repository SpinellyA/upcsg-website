using FastEndpoints;
using Microsoft.Extensions.Options;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

/// <summary>
/// Checks what actually landed in storage, and deletes it if it isn't acceptable.
///
/// This step exists because a presigned URL uploads without the API seeing the bytes. The
/// signature binds the content type, but nothing binds the SIZE — so an officer could
/// upload a 200 MB file and quietly consume the whole free tier. Reading the object back
/// is the only honest way to know what is really there.
/// </summary>
public class ConfirmUploadEndpoint(IMediaStore media, IOptions<MediaOptions> options)
    : Endpoint<ConfirmUploadRequest, ConfirmUploadDto>
{
    public override void Configure()
    {
        Post("/media/confirm");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(ConfirmUploadRequest req, CancellationToken ct)
    {
        var stored = await media.InspectAsync(req.Key, ct);

        if (stored is null)
        {
            AddError("That upload never arrived. Try again.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        var limit = options.Value.MaxUploadBytes;

        if (stored.SizeBytes > limit)
        {
            // Don't leave the oversized object sitting in the bucket costing storage.
            await media.DeleteAsync(req.Key, ct);

            AddError($"That image is {stored.SizeBytes / 1024 / 1024} MB. The limit is {limit / 1024 / 1024} MB.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (!MediaKeys.IsAllowedType(stored.ContentType))
        {
            await media.DeleteAsync(req.Key, ct);

            AddError("That file isn't an image.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.OkAsync(new ConfirmUploadDto
        {
            Key = req.Key,
            PublicUrl = media.PublicUrl(req.Key),
            SizeBytes = stored.SizeBytes,
        }, ct);
    }
}
