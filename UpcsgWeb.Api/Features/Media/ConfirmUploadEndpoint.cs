using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Media.ConfirmUpload;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Media;

public class ConfirmUploadEndpoint(ISender sender)
    : Endpoint<ConfirmUploadRequest, ConfirmUploadDto>
{
    public override void Configure()
    {
        Post("/media/confirm");

        Roles(UpcsgRoles.Member, UpcsgRoles.Admin);
    }

    public override async Task HandleAsync(ConfirmUploadRequest req, CancellationToken ct)
    {
        var confirmed = await sender.Send(
            new ConfirmUploadCommand(req.Key, User.IsInRole(UpcsgRoles.Admin)), ct);

        await Send.OkAsync(confirmed, ct);
    }
}
