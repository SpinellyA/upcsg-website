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

/// <summary>Wire-level mirror of the domain's OrderLineStatus.</summary>
public enum OrderLineStatusDto
{
    ToFulfil,
    RefundDue,
    Refunded,
}

public class OrderLineDto
{
    public int MerchItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public OrderLineStatusDto Status { get; set; }

    /// <summary>Why this line could not be filled. Shown to the guilder verbatim.</summary>
    public string? ShortfallReason { get; set; }

    public bool IsRefundDue => Status == OrderLineStatusDto.RefundDue;
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

    /// <summary>What was actually handed over. Null until an officer confirms the receipt.</summary>
    public decimal? AmountPaid { get; set; }

    /// <summary>Owed back for lines that could not be filled and have not been settled.</summary>
    public decimal RefundDue { get; set; }

    /// <summary>What the guilder is actually receiving, after shortfalls.</summary>
    public decimal FulfilledTotal { get; set; }

    /// <summary>GCash reference for money sent back. Null until it has been.</summary>
    public string? RefundReference { get; set; }

    public DateTimeOffset? RefundSettledAt { get; set; }

    public bool HasRefundDue => RefundDue > 0m;

    public bool RefundHasBeenSettled => RefundReference is not null;
}

/// <summary>Officer records that a refund actually went out.</summary>
public class SettleRefundRequest
{
    public string Reference { get; set; } = string.Empty;
}

/// <summary>Officer fills a previously short line after a restock.</summary>
public class RefulfilLineRequest
{
    public int MerchItemId { get; set; }
    public string? Variant { get; set; }
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

    /// <summary>
    /// Acknowledge even though stock is short, owing the difference back. Off by default:
    /// creating a refund obligation must be a deliberate act, not what happens when a
    /// field is left alone.
    /// </summary>
    public bool AllowShortfall { get; set; }
}
