using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Media.CreateUploadGrant;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

public class CreateUploadGrantEndpoint(ISender sender)
    : Endpoint<UploadGrantRequest, UploadGrantDto>
{
    public override void Configure()
    {
        Post("/media/upload-url");

        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);
    }

    public override async Task HandleAsync(UploadGrantRequest req, CancellationToken ct)
    {
        var grant = await sender.Send(
            new CreateUploadGrantCommand(
                req.Folder,
                req.FileName,
                req.ContentType,
                User.IsInRole(UpcsgRoles.Admin)),
            ct);

        await Send.OkAsync(grant, ct);
    }
}
