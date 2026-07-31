using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IOrderService
{
    Task<List<OrderDto>> GetMineAsync();
    Task<OrderDto?> GetAsync(int id);
    Task<OrderDto> SubmitReceiptAsync(int orderId, string reference, string? screenshotUrl);

    // Officer actions
    Task<List<OrderDto>> GetQueueAsync(OrderStatusDto? status);
    Task<OrderDto> ChangeStatusAsync(int orderId, OrderStatusDto status, string? reason, bool allowShortfall = false);
    Task<OrderDto> RejectReceiptAsync(int orderId, string reason);
    Task<OrderDto> SettleRefundAsync(int orderId, string reference);
    Task<OrderDto> RefulfilLineAsync(int orderId, int merchItemId, string? variant);
    Task<ReleaseConfirmedDto> ReleaseConfirmedAsync();
}

public class OrderService(HttpClient http, ApiOptions options) : IOrderService
{
    public async Task<List<OrderDto>> GetMineAsync()
    {
        EnsureConfigured();
        return await http.GetFromJsonAsync<List<OrderDto>>("api/orders/mine", UpcsgJson.Options) ?? [];
    }

    public async Task<OrderDto?> GetAsync(int id)
    {
        EnsureConfigured();

        var response = await http.GetAsync($"api/orders/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrderDto>(UpcsgJson.Options)
            : null;
    }

    public async Task<OrderDto> SubmitReceiptAsync(int orderId, string reference, string? screenshotUrl)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync($"api/orders/{orderId}/receipt", new SubmitReceiptRequest
        {
            ReferenceNumber = reference,
            ScreenshotUrl = screenshotUrl,
        }, UpcsgJson.Options);

        return await ReadOrThrowAsync(response);
    }

    public async Task<List<OrderDto>> GetQueueAsync(OrderStatusDto? status)
    {
        EnsureConfigured();

        // No status means "everything still open", which is the officer default.
        var url = status is null ? "api/orders" : $"api/orders?status={status}";
        return await http.GetFromJsonAsync<List<OrderDto>>(url, UpcsgJson.Options) ?? [];
    }

    public async Task<OrderDto> ChangeStatusAsync(int orderId, OrderStatusDto status, string? reason, bool allowShortfall = false)
    {
        EnsureConfigured();

        var response = await http.PatchAsJsonAsync($"api/orders/{orderId}/status", new ChangeOrderStatusRequest
        {
            Status = status,
            Reason = reason,
            AllowShortfall = allowShortfall,
        }, UpcsgJson.Options);

        return await ReadOrThrowAsync(response);
    }

    public async Task<OrderDto> SettleRefundAsync(int orderId, string reference)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync($"api/orders/{orderId}/settle-refund",
            new SettleRefundRequest { Reference = reference }, UpcsgJson.Options);

        return await ReadOrThrowAsync(response);
    }

    public async Task<OrderDto> RefulfilLineAsync(int orderId, int merchItemId, string? variant)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync($"api/orders/{orderId}/refulfil",
            new RefulfilLineRequest { MerchItemId = merchItemId, Variant = variant }, UpcsgJson.Options);

        return await ReadOrThrowAsync(response);
    }

    public async Task<ReleaseConfirmedDto> ReleaseConfirmedAsync()
    {
        EnsureConfigured();

        var response = await http.PostAsync("api/orders/release-confirmed", null);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<ReleaseConfirmedDto>(UpcsgJson.Options)
            ?? throw new ApiException("The API returned no result.");
    }

    public async Task<OrderDto> RejectReceiptAsync(int orderId, string reason)
    {
        EnsureConfigured();

        var response = await http.PostAsJsonAsync($"api/orders/{orderId}/receipt/reject",
            new RejectReceiptRequest { Reason = reason }, UpcsgJson.Options);

        return await ReadOrThrowAsync(response);
    }

    private void EnsureConfigured()
    {
        if (!options.IsConfigured)
        {
            throw new ApiException("No API is configured. Set Api:BaseUrl in wwwroot/appsettings.json.");
        }
    }

    private static async Task<OrderDto> ReadOrThrowAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            // 409 carries the domain's explanation of why the transition was refused.
            throw new ApiException(await CartService.DescribeAsync(response));
        }

        return await response.Content.ReadFromJsonAsync<OrderDto>(UpcsgJson.Options)
            ?? throw new ApiException("The API returned no order.");
    }
}
