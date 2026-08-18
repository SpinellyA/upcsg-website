namespace UpcsgWeb.Shared.Contracts;

public enum OrderStatusDto
{
    AwaitingPayment,
    Pending,
    Acknowledged,
    Released,
    Received,
    Cancelled,
}

public enum PaymentMethodDto
{
    /// <summary>Handed over in person and recorded by an officer. Holds no stock until then.</summary>
    Cash,

    /// <summary>Paid online; the reference is taken at face value and confirmed at once.</summary>
    GCash,
}

public enum OrderLineStatusDto
{
    ToFulfil,
    RefundDue,
    Refunded,
}

public class OrderLineDto
{
    public Guid MerchItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }

    public OrderLineStatusDto Status { get; set; }

    public string? ShortfallReason { get; set; }

    public bool IsRefundDue => Status == OrderLineStatusDto.RefundDue;
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string? GuilderName { get; set; }

    public string? GuilderEmail { get; set; }

    public string Reference => OrderReference.For(Id);

    public OrderStatusDto Status { get; set; }

    public PaymentMethodDto PaymentMethod { get; set; }

    public bool IsCash => PaymentMethod == PaymentMethodDto.Cash;

    /// <summary>
    /// A cash order sitting in the queue: paid for in person, or about to be, and waiting for
    /// an officer to record it. Nothing is held for it until they do.
    /// </summary>
    public bool AwaitsCashCollection =>
        IsCash && Status == OrderStatusDto.Pending;

    public DateTimeOffset PlacedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Note { get; set; }
    public string? CancellationReason { get; set; }
    public string? ReceiptRejectionReason { get; set; }
    public PaymentReceiptDto? Receipt { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "PHP";
    public List<OrderLineDto> Lines { get; set; } = [];

    public decimal? AmountPaid { get; set; }

    public decimal RefundDue { get; set; }

    public decimal FulfilledTotal { get; set; }

    public string? RefundReference { get; set; }

    public DateTimeOffset? RefundSettledAt { get; set; }

    public bool HasRefundDue => RefundDue > 0m;

    public bool RefundHasBeenSettled => RefundReference is not null;
}

public class SettleRefundRequest
{
    public string Reference { get; set; } = string.Empty;
}

public class RefulfilLineRequest
{
    public Guid MerchItemId { get; set; }
    public string? Variant { get; set; }
}

public class ReleaseConfirmedDto
{
    public int ReleasedCount { get; set; }

    public List<Guid> ReleasedOrderIds { get; set; } = [];

    public List<string> Skipped { get; set; } = [];
}

public class PlaceOrderRequest
{
    public string? Note { get; set; }
    public List<PlaceOrderLine> Lines { get; set; } = [];
}

public class PlaceOrderLine
{
    public Guid MerchItemId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; } = 1;
}

public class ChangeOrderStatusRequest
{
    public OrderStatusDto Status { get; set; }

    public string? Reason { get; set; }

    public bool AllowShortfall { get; set; }
}
