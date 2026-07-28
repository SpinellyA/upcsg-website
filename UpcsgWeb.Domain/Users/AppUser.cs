using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Users;

/// <summary>
/// A signed-in guilder. Root of its own aggregate â€” orders point at it by id rather
/// than hanging off it, so loading a user never drags their order history along.
/// </summary>
public class AppUser : AggregateRoot
{
    private AppUser() { } // EF

    /// <summary>Google's stable subject id. Survives the user changing their email.</summary>
    public string GoogleSubject { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? PictureUrl { get; private set; }
    public string Role { get; private set; } = GuildRoles.Member;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;

    public bool IsAdmin => Role == GuildRoles.Admin;

    public static AppUser Register(string googleSubject, string email, string name, string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
        {
            throw new DomainException("A user must have a Google subject id.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("A user must have an email address.");
        }

        return new AppUser
        {
            GoogleSubject = googleSubject,
            Email = email,
            Name = string.IsNullOrWhiteSpace(name) ? email : name,
            PictureUrl = pictureUrl,

            // Everyone starts as a member. Promotion is a separate, deliberate act â€”
            // see GrantAdmin. Signing in can never confer it.
            Role = GuildRoles.Member,
        };
    }

    /// <summary>Refreshes profile fields on each sign-in. Never touches Role.</summary>
    public void RefreshProfile(string email, string name, string? pictureUrl)
    {
        Email = email;
        Name = string.IsNullOrWhiteSpace(name) ? email : name;
        PictureUrl = pictureUrl;
        LastLoginAt = DateTime.UtcNow;
    }

    public void GrantAdmin() => Role = GuildRoles.Admin;

    public void RevokeAdmin() => Role = GuildRoles.Member;
}
