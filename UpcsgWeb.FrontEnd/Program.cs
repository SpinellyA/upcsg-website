using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using UpcsgWeb.FrontEnd;
using UpcsgWeb.FrontEnd.Auth;
using UpcsgWeb.FrontEnd.Http;
using UpcsgWeb.FrontEnd.Services;
using UpcsgWeb.Shared.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// --- API configuration ---------------------------------------------------------------
// Read from wwwroot/appsettings.json. Empty BaseUrl keeps the public site on seed data.
var apiOptions = new ApiOptions();
builder.Configuration.GetSection("Api").Bind(apiOptions);
builder.Services.AddSingleton(apiOptions);

var apiBase = apiOptions.IsConfigured
    ? apiOptions.BaseUrl.TrimEnd('/') + "/"
    : builder.HostEnvironment.BaseAddress;

// Session storage is registered before HttpClient and depends only on JS interop, which
// is what keeps the token handler out of HttpClient's own dependency graph.
builder.Services.AddScoped<ISessionStore, SessionStore>();

// Every API call goes through the handler so the bearer token is never forgotten.
// The handler is constructed here rather than resolved, so its InnerHandler is assigned
// exactly once per client instead of being mutated on a shared scoped object.
builder.Services.AddScoped(sp =>
{
    var handler = new AuthTokenHandler(sp.GetRequiredService<ISessionStore>())
    {
        InnerHandler = new HttpClientHandler(),
    };

    return new HttpClient(handler) { BaseAddress = new Uri(apiBase) };
});

// --- Auth ------------------------------------------------------------------------------
builder.Services.AddAuthorizationCore(options =>
{
    // Admin surfaces opt in with [Authorize(Policy = "ExeCom")].
    options.AddPolicy("ExeCom", policy => policy.RequireRole(UpcsgRoles.Admin));
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UpcsgAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<UpcsgAuthenticationStateProvider>());

// --- Content (live when configured, seeded otherwise) -----------------------------------
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IMerchService, MerchService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();

// --- Server-only features ---------------------------------------------------------------
// Carts, orders and the CMS have no meaningful offline mode; they surface a clear
// message when no API is configured rather than pretending to work.
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminContentService, AdminContentService>();

// Image uploads: signs, sends and confirms. Needs HttpClient and JS interop only.
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();

await builder.Build().RunAsync();
