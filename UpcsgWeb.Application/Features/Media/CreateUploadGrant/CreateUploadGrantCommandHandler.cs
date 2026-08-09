using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Media.CreateUploadGrant;

public class CreateUploadGrantCommandHandler(IMediaStore media)
    : ICommandHandler<CreateUploadGrantCommand, UploadGrantDto>
{
    public async Task<UploadGrantDto> Handle(
        CreateUploadGrantCommand command,
        CancellationToken cancellationToken)
    {
        if (!MediaKeys.IsMemberWritableFolder(command.Folder) && !command.IsOfficer)
        {
            throw new ForbiddenException("Only officers may upload site content.");
        }

        var grant = await media.CreateUploadGrantAsync(
            command.Folder, command.FileName, command.ContentType, cancellationToken);

        return new UploadGrantDto
        {
            Key = grant.Key,
            UploadUrl = grant.UploadUrl,
            PublicUrl = grant.PublicUrl,
            Method = grant.Method,
        };
    }
}
