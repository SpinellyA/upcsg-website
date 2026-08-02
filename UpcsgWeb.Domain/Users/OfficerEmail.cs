using System.Text.RegularExpressions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Users;

/// <summary>
/// An email address that gets officer rights when it signs in.
///
/// Roles are not editable per user on purpose. Google is the identity provider, so the
/// only thing the guild actually controls is which addresses count as officers — and an
/// allowlist is something the outgoing ExeCom can hand over, unlike a database flag
/// somebody has to remember to flip.
///
/// The address is stored normalised. Google returns whatever casing the user typed, so
/// matching raw text would let "Officer@up.edu.ph" sign in as a member while
/// "officer@up.edu.ph" sits in the list.
/// </summary>
public partial class OfficerEmail : AggregateRoot
{
    private OfficerEmail() { } // EF

    public string Email { get; private set; } = string.Empty;

    /// <summary>Who this is, so a handover doesn't leave a list of anonymous addresses.</summary>
    public string? Note { get; private set; }

    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    public static OfficerEmail Create(string email, string? note)
    {
        var normalised = Normalise(email);

        if (normalised.Length == 0)
        {
            throw new DomainException("An officer email is required.");
        }

        // Deliberately permissive: this is a typo guard, not an RFC parser. The address
        // still has to match a real Google account before it grants anything.
        if (!EmailShape().IsMatch(normalised))
        {
            throw new DomainException($"'{email}' does not look like an email address.");
        }

        return new OfficerEmail
        {
            Id = Guid.CreateVersion7(),
            Email = normalised,
            Note = Clean(note),
        };
    }

    public void Describe(string? note) => Note = Clean(note);

    /// <summary>
    /// Lower-cased and trimmed. Every comparison against a signed-in address must go
    /// through here, or the allowlist silently misses on casing alone.
    /// </summary>
    public static string Normalise(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string? Clean(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailShape();
}
