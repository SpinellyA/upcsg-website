using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using UpcsgWeb.FrontEnd.Http;
using UpcsgWeb.FrontEnd.Services;

namespace UpcsgWeb.Domain.Tests;

public class ServiceGraphTests
{
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

        services.AddLogging();

        services.AddScoped<ISnapshotService>(sp => new SnapshotService(
            new HttpClient { BaseAddress = new Uri("http://localhost:5000/") },
            sp.GetRequiredService<ILogger<SnapshotService>>()));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMerchService, MerchService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAdminContentService, AdminContentService>();

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
    [InlineData(typeof(ISnapshotService))]
    [InlineData(typeof(ISessionStore))]
    public void EveryRegisteredServiceResolves(Type serviceType)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }
}

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
