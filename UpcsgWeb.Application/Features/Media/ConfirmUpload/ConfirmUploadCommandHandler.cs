using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Media;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Media.ConfirmUpload;

public class ConfirmUploadCommandHandler(IMediaStore media, MediaLimits limits)
    : ICommandHandler<ConfirmUploadCommand, ConfirmUploadDto>
{
    public async Task<ConfirmUploadDto> Handle(
        ConfirmUploadCommand command,
        CancellationToken cancellationToken)
    {
        if (!MediaKeys.IsReceiptKey(command.Key) && !command.IsOfficer)
        {
            throw new ForbiddenException("Only officers may confirm site content.");
        }

        var stored = await media.InspectAsync(command.Key, cancellationToken)
            ?? throw new NotFoundException("That upload");

        if (stored.SizeBytes > limits.MaxUploadBytes)
        {
            await media.DeleteAsync(command.Key, cancellationToken);

            throw new DomainException(
                $"That image is {stored.SizeBytes / 1024 / 1024} MB. "
                + $"The limit is {limits.MaxUploadBytes / 1024 / 1024} MB.");
        }

        if (!MediaKeys.IsAllowedType(stored.ContentType))
        {
            await media.DeleteAsync(command.Key, cancellationToken);

            throw new DomainException("That file isn't an image.");
        }

        var isPrivate = media.IsPrivate(command.Key);
        var publicUrl = isPrivate ? string.Empty : media.PublicUrl(command.Key);

        return new ConfirmUploadDto
        {
            Key = command.Key,
            PublicUrl = publicUrl,
            StoredReference = isPrivate ? command.Key : publicUrl,
            SizeBytes = stored.SizeBytes,
        };
    }
}
