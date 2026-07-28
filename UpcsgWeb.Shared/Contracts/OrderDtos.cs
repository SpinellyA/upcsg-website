namespace UpcsgWeb.Shared.Contracts;

/// <summary>Wire-level mirror of the domain's OrderStatus.</summary>
public enum OrderStatusDto
{
    AwaitingPayment,
    Pending,
    Acknowledged,
    Released,
    Received,
    Cancelled,
}

public class OrderLineDto
{
    public int MerchItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public OrderStatusDto Status { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Note { get; set; }
    public string? CancellationReason { get; set; }
    public string? ReceiptRejectionReason { get; set; }
    public PaymentReceiptDto? Receipt { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "PHP";
    public List<OrderLineDto> Lines { get; set; } = [];
}

/// <summary>
/// What a guilder submits. Deliberately carries no price — the server snapshots the
/// current one, so a client cannot name its own.
/// </summary>
public class PlaceOrderRequest
{
    public string? Note { get; set; }
    public List<PlaceOrderLine> Lines { get; set; } = [];
}

public class PlaceOrderLine
{
    public int MerchItemId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Officer-driven status move. The domain decides whether it's legal.</summary>
public class ChangeOrderStatusRequest
{
    public OrderStatusDto Status { get; set; }

    /// <summary>Required when cancelling.</summary>
    public string? Reason { get; set; }
}
