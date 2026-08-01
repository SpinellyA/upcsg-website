using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Shared handle to the motion module.
///
/// Registered as a singleton so the module is imported once per app rather than once per
/// component: a page with twenty revealed elements would otherwise issue twenty dynamic
/// imports of the same file. The <see cref="Lazy{T}"/> also means a page that animates
/// nothing never fetches the script at all.
/// </summary>
public class MotionInterop(IJSRuntime js) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module = new(() =>
        js.InvokeAsync<IJSObjectReference>("import", "./js/motion.js").AsTask());

    public async Task RevealAsync(ElementReference element) =>
        await CallAsync("reveal", element);

    public async Task ReleaseAsync(ElementReference element) =>
        await CallAsync("release", element);

    public async Task CountUpAsync(ElementReference element, int target, int durationMs) =>
        await CallAsync("countUp", element, target, durationMs);

    public async Task TrackScrollAsync(ElementReference element) =>
        await CallAsync("trackScroll", element);

    private async Task CallAsync(string method, params object?[] args)
    {
        try
        {
            var module = await _module.Value;
            await module.InvokeVoidAsync(method, args);
        }
        catch (JSDisconnectedException)
        {
            // The circuit or tab went away mid-navigation. Decoration failing is never
            // worth surfacing to the user, and never worth taking the page down for.
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_module.IsValueCreated)
        {
            return;
        }

        try
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }

        GC.SuppressFinalize(this);
    }
}
