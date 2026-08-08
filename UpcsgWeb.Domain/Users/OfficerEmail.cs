using System.Text.RegularExpressions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Users;

public partial class OfficerEmail : AggregateRoot
{
    private OfficerEmail() { }

    public string Email { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;

    public static OfficerEmail Create(string email, string? note)
    {
        var normalised = Normalise(email);

        if (normalised.Length == 0)
        {
            throw new DomainException("An officer email is required.");
        }

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

    public static string Normalise(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string? Clean(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailShape();
}
