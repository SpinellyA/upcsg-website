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

var apiOptions = new ApiOptions();
builder.Configuration.GetSection("Api").Bind(apiOptions);
builder.Services.AddSingleton(apiOptions);

var googleOptions = new GoogleAuthOptions();
builder.Configuration.GetSection("Google").Bind(googleOptions);
builder.Services.AddSingleton(googleOptions);

var apiBase = apiOptions.IsConfigured
    ? apiOptions.BaseUrl.TrimEnd('/') + "/"
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<ISessionStore, SessionStore>();

builder.Services.AddScoped(sp =>
{
    var handler = new AuthTokenHandler(sp.GetRequiredService<ISessionStore>())
    {
        InnerHandler = new HttpClientHandler(),
    };

    return new HttpClient(handler) { BaseAddress = new Uri(apiBase) };
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("ExeCom", policy => policy.RequireRole(UpcsgRoles.Admin));
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UpcsgAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<UpcsgAuthenticationStateProvider>());

builder.Services.AddScoped<ISnapshotService>(sp => new SnapshotService(
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) },
    sp.GetRequiredService<ILogger<SnapshotService>>()));

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IMerchService, MerchService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminContentService, AdminContentService>();

builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();

builder.Services.AddSingleton<MotionInterop>();

await builder.Build().RunAsync();
