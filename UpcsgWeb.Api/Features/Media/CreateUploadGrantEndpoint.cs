using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

public class CreateUploadGrantEndpoint(IMediaStore media)
    : Endpoint<UploadGrantRequest, UploadGrantDto>
{
    public override void Configure()
    {
        Post("/media/upload-url");

        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);
    }

    public override async Task HandleAsync(UploadGrantRequest req, CancellationToken ct)
    {
        if (!MediaKeys.IsAllowedFolder(req.Folder))
        {
            AddError("Unknown media folder.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        if (!MediaKeys.IsMemberWritableFolder(req.Folder) && !User.IsInRole(UpcsgRoles.Admin))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!MediaKeys.IsAllowedType(req.ContentType))
        {
            AddError($"Images only — {string.Join(", ", MediaKeys.AllowedContentTypes)}.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var grant = await media.CreateUploadGrantAsync(req.Folder, req.FileName, req.ContentType, ct);

        await Send.OkAsync(new UploadGrantDto
        {
            Key = grant.Key,
            UploadUrl = grant.UploadUrl,
            PublicUrl = grant.PublicUrl,
            Method = grant.Method,
        }, ct);
    }
}
