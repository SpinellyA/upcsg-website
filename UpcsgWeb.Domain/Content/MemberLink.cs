using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Content;

public enum MemberLinkKind
{
    Email,
    Facebook,
    Instagram,
    LinkedIn,
    GitHub,
    Website,
}

// A way to reach someone. The kind is an enum rather than a free-text label so the
// profile can pick the right icon and build the right href - an email is a mailto,
// everything else is a URL - instead of trusting whatever an officer typed.
public class MemberLink
{
    private MemberLink() { }

    public MemberLinkKind Kind { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public static MemberLink Of(MemberLinkKind kind, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("A contact needs an address.");
        }

        var trimmed = value.Trim();

        if (kind == MemberLinkKind.Email)
        {
            if (!trimmed.Contains('@') || trimmed.StartsWith('@') || trimmed.EndsWith('@'))
            {
                throw new DomainException($"\"{trimmed}\" is not an email address.");
            }
        }
        else if (!IsWebAddress(trimmed))
        {
            throw new DomainException($"\"{trimmed}\" must be a full http or https link.");
        }

        return new MemberLink { Kind = kind, Value = trimmed };
    }

    private static bool IsWebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var parsed)
        && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
