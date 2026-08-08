namespace UpcsgWeb.Api.Auth;

public record GoogleIdentity(
    string Subject,
    string Email,
    string Name,
    string? PictureUrl,
    string? HostedDomain);
