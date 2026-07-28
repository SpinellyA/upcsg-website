namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Where the API lives, read from wwwroot/appsettings.json at startup.
///
/// Empty means "no API configured": the site falls back to seed data so the public
/// pages still render standalone. Cart and admin need a real API and say so.
/// </summary>
public class ApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
