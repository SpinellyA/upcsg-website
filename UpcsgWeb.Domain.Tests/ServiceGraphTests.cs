using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using UpcsgWeb.FrontEnd.Http;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.Domain.Tests;

/// <summary>
/// Guards the client's dependency graph.
///
/// Regression cover for a startup hang: AuthTokenHandler used to depend on IAuthService,
/// which depends on HttpClient, which is built *from* the handler. Because that cycle ran
/// through a factory delegate, DI could not detect it — resolution simply recursed until
/// the WebAssembly stack overflowed and the app froze at "100%" with no error in console.
///
/// A build succeeds either way, so only actually resolving the graph catches it.
/// </summary>
public class ServiceGraphTests
{
    /// <summary>Mirrors the registrations in Program.cs.</summary>
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new ApiOptions { BaseUrl = "http://localhost:5180" });
        services.AddSingleton(Mock.JsRuntime);

        services.AddScoped<ISessionStore, SessionStore>();

        services.AddScoped(sp =>
        {
            var handler = new AuthTokenHandler(sp.GetRequiredService<ISessionStore>())
            {
                InnerHandler = new HttpClientHandler(),
            };

            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5180/") };
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMerchService, MerchService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminContentService, AdminContentService>();

        // validateScopes/validateOnBuild surface graph problems eagerly rather than on
        // first navigation, which is where the original hang happened.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true,
        });
    }

    [Fact]
    public void HttpClientResolvesWithoutRecursion()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

        Assert.NotNull(http);
        Assert.Equal("http://localhost:5180/", http.BaseAddress!.ToString());
    }

    [Fact]
    public void AuthServiceAndTokenHandlerCoexist()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // The pair that used to deadlock the container.
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

        Assert.NotNull(auth);
        Assert.NotNull(http);
    }

    [Theory]
    [InlineData(typeof(IEventService))]
    [InlineData(typeof(IMerchService))]
    [InlineData(typeof(IMemberService))]
    [InlineData(typeof(IAchievementService))]
    [InlineData(typeof(ICartService))]
    [InlineData(typeof(IOrderService))]
    [InlineData(typeof(IAdminContentService))]
    [InlineData(typeof(ISessionStore))]
    public void EveryRegisteredServiceResolves(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }
}

/// <summary>Minimal IJSRuntime stand-in; these tests never invoke JS.</summary>
internal static class Mock
{
    public static IJSRuntime JsRuntime { get; } = new NoopJsRuntime();

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult<TValue>(default!);
    }
}
