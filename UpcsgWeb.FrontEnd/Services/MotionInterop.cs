using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace UpcsgWeb.FrontEnd.Services;

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
