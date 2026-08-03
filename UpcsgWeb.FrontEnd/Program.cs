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

// The Google client id, also runtime configuration. Empty means the login page offers
// the development stand-ins instead of the real button.
var googleOptions = new GoogleAuthOptions();
builder.Configuration.GetSection("Google").Bind(googleOptions);
builder.Services.AddSingleton(googleOptions);

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

// --- Content (live when reachable, snapshot otherwise) ----------------------------------
// The snapshot is served by the site, not the API, so it needs its own client: the shared
// one is based at the API origin, and the whole point of the fallback is that it works
// when that origin is unreachable. Requesting it through the API client asks a dead host
// for the file that exists to survive the host being dead.
//
// Scoped rather than singleton because HttpClient is scoped, and a singleton holding a
// scoped dependency fails container validation at startup. In WebAssembly a scope lasts
// as long as the tab, so the snapshot is still fetched once per visit.
builder.Services.AddScoped<ISnapshotService>(sp => new SnapshotService(
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) },
    sp.GetRequiredService<ILogger<SnapshotService>>()));

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

// Scroll-triggered animation. Singleton so the JS module is imported once for the whole
// app instead of once per animated component.
builder.Services.AddSingleton<MotionInterop>();

await builder.Build().RunAsync();
