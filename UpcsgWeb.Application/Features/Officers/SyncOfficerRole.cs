using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Features.Officers;

public static class SyncOfficerRole
{
    public static async Task<bool> ApplyAsync(
        IUnitOfWork uow,
        AppUser user,
        CancellationToken ct = default)
    {
        var shouldBeOfficer = await uow.OfficerEmails.IsOfficerAsync(
            OfficerEmail.Normalise(user.Email), ct);

        if (shouldBeOfficer && !user.IsAdmin)
        {
            user.GrantAdmin();
            return true;
        }

        if (!shouldBeOfficer && user.IsAdmin)
        {
            user.RevokeAdmin();
            return true;
        }

        return false;
    }
}
