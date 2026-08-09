using FluentValidation;
using UpcsgWeb.Domain.Media;

namespace UpcsgWeb.Application.Features.Media.CreateUploadGrant;

public class CreateUploadGrantValidator : AbstractValidator<CreateUploadGrantCommand>
{
    public CreateUploadGrantValidator()
    {
        RuleFor(c => c.Folder)
            .Must(MediaKeys.IsAllowedFolder)
            .WithMessage("Unknown media folder.");

        RuleFor(c => c.FileName)
            .NotEmpty()
            .WithMessage("A file name is required.");

        RuleFor(c => c.ContentType)
            .Must(MediaKeys.IsAllowedType)
            .WithMessage($"Images only — {string.Join(", ", MediaKeys.AllowedContentTypes)}.");
    }
}
