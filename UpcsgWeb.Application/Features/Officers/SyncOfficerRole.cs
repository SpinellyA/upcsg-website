using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Application.Features.Officers;

/// <summary>
/// Brings a user's role in line with the officer allowlist.
///
/// Called from two places, which is the whole point of it existing: at sign-in, so a
/// newly added officer gets their rights the moment they log in, and when the list is
/// edited, so an address that is added or removed takes effect against an existing
/// account immediately rather than at some future sign-in.
///
/// Not a command: it never saves. The caller owns the unit of work, so the role change
/// commits inside whatever transaction it is already part of.
/// </summary>
public static class SyncOfficerRole
{
    /// <summary>Returns true when the role actually changed.</summary>
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

        // Demotion matters more than promotion: an officer who has handed over should
        // stop being one, and leaving the role behind is how a graduated ExeCom keeps
        // write access to the site for years.
        if (!shouldBeOfficer && user.IsAdmin)
        {
            user.RevokeAdmin();
            return true;
        }

        return false;
    }
}
