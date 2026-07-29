using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Infrastructure.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

/// <summary>
/// Hands the browser permission to upload one image directly to storage.
///
/// Officers only. The folder and content type are checked here rather than trusted, and
/// the key is generated server-side so a caller cannot choose where their file lands or
/// overwrite somebody else's.
/// </summary>
public class CreateUploadGrantEndpoint(IMediaStore media)
    : Endpoint<UploadGrantRequest, UploadGrantDto>
{
    public override void Configure()
    {
        Post("/media/upload-url");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(UploadGrantRequest req, CancellationToken ct)
    {
        if (!MediaKeys.IsAllowedFolder(req.Folder))
        {
            AddError("Unknown media folder.");
            await Send.ErrorsAsync(400, ct);
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
