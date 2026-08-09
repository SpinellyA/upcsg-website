using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Media.CreateUploadGrant;

public record CreateUploadGrantCommand(
    string Folder,
    string FileName,
    string ContentType,
    bool IsOfficer) : ICommand<UploadGrantDto>;
