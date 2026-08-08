namespace UpcsgWeb.FrontEnd.Services;

public class ApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
