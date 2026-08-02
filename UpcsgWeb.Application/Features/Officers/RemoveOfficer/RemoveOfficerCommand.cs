using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Application.Features.Officers.RemoveOfficer;

public record RemoveOfficerCommand(Guid Id) : ICommand;

public class RemoveOfficerCommandHandler(IUnitOfWork uow) : ICommandHandler<RemoveOfficerCommand>
{
    public async Task Handle(RemoveOfficerCommand command, CancellationToken cancellationToken)
    {
        var officer = await uow.OfficerEmails.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("That officer email");

        // The only route back from an empty allowlist is someone with database access,
        // and the development sign-in endpoint that could mint an admin token does not
        // exist in production. Removing the last officer is unrecoverable from inside
        // the app, so it is refused rather than confirmed.
        if (await uow.OfficerEmails.CountAsync(cancellationToken) <= 1)
        {
            throw new DomainException(
                "This is the last officer email. Add another before removing this one, "
                + "or nobody will be able to administer the site.");
        }

        uow.OfficerEmails.Remove(officer);

        // Demote the matching account in the same transaction. Leaving it would mean the
        // list says they are not an officer while the site still treats them as one.
        var user = await uow.Users.GetByEmailAsync(officer.Email, cancellationToken);
        user?.RevokeAdmin();

        await uow.SaveChangesAsync(cancellationToken);
    }
}
