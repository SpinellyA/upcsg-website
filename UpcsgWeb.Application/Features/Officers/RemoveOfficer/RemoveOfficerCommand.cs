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

        if (await uow.OfficerEmails.CountAsync(cancellationToken) <= 1)
        {
            throw new DomainException(
                "This is the last officer email. Add another before removing this one, "
                + "or nobody will be able to administer the site.");
        }

        uow.OfficerEmails.Remove(officer);

        var user = await uow.Users.GetByEmailAsync(officer.Email, cancellationToken);
        user?.RevokeAdmin();

        await uow.SaveChangesAsync(cancellationToken);
    }
}
