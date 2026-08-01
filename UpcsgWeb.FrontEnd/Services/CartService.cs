using System.Net;
using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface ICartService
{
    Task<CartDto> GetAsync();
    Task<CartDto> AddAsync(Guid merchItemId, string? variant, int quantity);
    Task<CartDto> SetQuantityAsync(Guid merchItemId, string? variant, int quantity);
    Task ClearAsync();
    Task<OrderDto> CheckoutAsync(string? note);

    /// <summary>Item count for the nav badge. Zero when signed out or unconfigured.</summary>
    Task<int> GetItemCountAsync();

    /// <summary>Raised after any mutation so the nav badge can refresh.</summary>
    event Action? Changed;
}

/// <summary>
/// Cart client. There is no seed fallback: a cart is inherently server state, and
/// pretending otherwise would let someone "check out" into nothing.
/// </summary>
public class CartService(HttpClient http, ApiOptions options) : ICartService
{
    public event Action? Changed;

    public async Task<CartDto> GetAsync()
    {
        EnsureConfigured();

        var response = await http.GetAsync("api/cart");

        // Signed out: an empty cart is the honest answer, not an error page.
        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            return new CartDto();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartDto>(UpcsgJson.Options) ?? new CartDto();
    }

    public async Task<CartDto> AddAsync(Guid merchItemId, string? variant, int quantity)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync("api/cart/items", new AddToCartRequest
        {
            MerchItemId = merchItemId,
            Variant = variant,
            Quantity = quantity,
        }, UpcsgJson.Options);

        var cart = await ReadOrThrowAsync(response);
        Changed?.Invoke();
        return cart;
    }

    public async Task<CartDto> SetQuantityAsync(Guid merchItemId, string? variant, int quantity)
    {
        EnsureConfigured();

        var response = await http.PatchAsJsonAsync("api/cart/items", new UpdateCartLineRequest
        {
            MerchItemId = merchItemId,
            Variant = variant,
            Quantity = quantity,
        }, UpcsgJson.Options);

        var cart = await ReadOrThrowAsync(response);
        Changed?.Invoke();
        return cart;
    }

    public async Task ClearAsync()
    {
        EnsureConfigured();

        var response = await http.DeleteAsync("api/cart");
        response.EnsureSuccessStatusCode();
        Changed?.Invoke();
    }

    public async Task<OrderDto> CheckoutAsync(string? note)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync("api/cart/checkout", new CheckoutRequest { Note = note }, UpcsgJson.Options);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await DescribeAsync(response));
        }

        var order = await response.Content.ReadFromJsonAsync<OrderDto>(UpcsgJson.Options)
            ?? throw new ApiException("Checkout returned no order.");

        Changed?.Invoke();
        return order;
    }

    public async Task<int> GetItemCountAsync()
    {
        if (!options.IsConfigured)
        {
            return 0;
        }

        try
        {
            var cart = await GetAsync();
            return cart.TotalItems;
        }
        catch
        {
            // The badge must never break the page it sits in.
            return 0;
        }
    }

    private void EnsureConfigured()
    {
        if (!options.IsConfigured)
        {
            throw new ApiException("No API is configured, so the cart is unavailable. Set Api:BaseUrl in wwwroot/appsettings.json.");
        }
    }

    private static async Task<CartDto> ReadOrThrowAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<CartDto>(UpcsgJson.Options) ?? new CartDto();
    }

    /// <summary>Surfaces the domain's own message instead of a bare status code.</summary>
    internal static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "Please sign in first.";
        }

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(UpcsgJson.Options);
            var first = problem?.Errors?.SelectMany(e => e.Value).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }
        catch
        {
            // Not a problem-details body; fall through.
        }

        return $"Request failed ({(int)response.StatusCode}).";
    }
}

/// <summary>Shape FastEndpoints uses for validation and domain failures.</summary>
public class ApiErrorResponse
{
    public Dictionary<string, List<string>>? Errors { get; set; }
}

public class ApiException(string message) : Exception(message);
