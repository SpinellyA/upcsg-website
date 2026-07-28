namespace UpcsgWeb.Api.Auth;

/// <summary>The verified claims we take from a Google ID token — nothing more.</summary>
public record GoogleIdentity(
    string Subject,
    string Email,
    string Name,
    string? PictureUrl,
    string? HostedDomain);
