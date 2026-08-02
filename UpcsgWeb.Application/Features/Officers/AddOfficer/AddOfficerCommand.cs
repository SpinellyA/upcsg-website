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

        // Checked rather than relying on the unique index: a duplicate is an ordinary
        // mistake, and it should read as "already on the list" rather than as a
        // constraint violation surfacing from the database.
        if (await uow.OfficerEmails.GetByEmailAsync(officer.Email, cancellationToken) is not null)
        {
            throw new DomainException($"{officer.Email} is already an officer.");
        }

        uow.OfficerEmails.Add(officer);

        // If that person already has an account, promote it now. Waiting for their next
        // sign-in would mean adding someone and watching nothing happen.
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
