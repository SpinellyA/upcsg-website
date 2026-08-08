using FastEndpoints;
using Microsoft.Extensions.Options;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

public class ConfirmUploadEndpoint(IMediaStore media, IOptions<MediaOptions> options)
    : Endpoint<ConfirmUploadRequest, ConfirmUploadDto>
{
    public override void Configure()
    {
        Post("/media/confirm");

        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);
    }

    public override async Task HandleAsync(ConfirmUploadRequest req, CancellationToken ct)
    {
        if (!MediaKeys.IsReceiptKey(req.Key) && !User.IsInRole(UpcsgRoles.Admin))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

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

        var isPrivate = media.IsPrivate(req.Key);

        await Send.OkAsync(new ConfirmUploadDto
        {
            Key = req.Key,
            PublicUrl = isPrivate ? string.Empty : media.PublicUrl(req.Key),
            StoredReference = isPrivate ? req.Key : media.PublicUrl(req.Key),
            SizeBytes = stored.SizeBytes,
        }, ct);
    }
}
