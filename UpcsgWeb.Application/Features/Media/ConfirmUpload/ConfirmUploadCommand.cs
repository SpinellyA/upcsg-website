using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Media.ConfirmUpload;

public record ConfirmUploadCommand(string Key, bool IsOfficer) : ICommand<ConfirmUploadDto>;
