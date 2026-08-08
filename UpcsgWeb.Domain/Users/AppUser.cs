using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Users;

public class AppUser : AggregateRoot
{
    private AppUser() { }

    public string GoogleSubject { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? PictureUrl { get; private set; }
    public string Role { get; private set; } = GuildRoles.Member;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; private set; } = DateTime.UtcNow;

    public bool IsAdmin => Role == GuildRoles.Admin;

    public static AppUser Create(string googleSubject, string email, string name, string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
        {
            throw new DomainException("A user must have a Google subject id.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("A user must have an email address.");
        }

        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            GoogleSubject = googleSubject,
            Email = email,
            Name = string.IsNullOrWhiteSpace(name) ? email : name,
            PictureUrl = pictureUrl,

            Role = GuildRoles.Member,
        };

        user.Raise(new UserRegisteredEvent(user.Id, user.Email));
        return user;
    }

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
