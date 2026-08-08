using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Officers.AddOfficer;

public record AddOfficerCommand(string Email, string? Note) : ICommand<OfficerEmailDto>;

public class AddOfficerCommandHandler(IUnitOfWork uow)
    : ICommandHandler<AddOfficerCommand, OfficerEmailDto>
{
    public async Task<OfficerEmailDto> Handle(
        AddOfficerCommand command,
        CancellationToken cancellationToken)
    {
        var officer = OfficerEmail.Create(command.Email, command.Note);

        if (await uow.OfficerEmails.GetByEmailAsync(officer.Email, cancellationToken) is not null)
        {
            throw new DomainException($"{officer.Email} is already an officer.");
        }

        uow.OfficerEmails.Add(officer);

        var existing = await uow.Users.GetByEmailAsync(officer.Email, cancellationToken);
        var promoted = false;

        if (existing is not null)
        {
            existing.GrantAdmin();
            promoted = true;
        }

        await uow.SaveChangesAsync(cancellationToken);

        return new OfficerEmailDto
        {
            Id = officer.Id,
            Email = officer.Email,
            Note = officer.Note,
            AddedAt = officer.AddedAt,
            HasSignedIn = promoted,
        };
    }
}
