namespace UpcsgWeb.Application.Abstractions;

public record GoogleIdentity(
    string Subject,
    string Email,
    string Name,
    string? PictureUrl,
    string? HostedDomain);

public interface IGoogleTokenVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string credential, CancellationToken ct);
}

public interface ITokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(
        Guid userId, string email, string name, string role, string? pictureUrl);
}

public sealed class SignInOptions
{
    public string? RequiredHostedDomain { get; set; }
}

public sealed class MediaLimits
{
    public long MaxUploadBytes { get; set; } = 8 * 1024 * 1024;
}
