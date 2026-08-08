namespace UpcsgWeb.FrontEnd.Services;

public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
