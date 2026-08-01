namespace UpcsgWeb.Shared.Contracts;

/// <summary>
/// A cart line as displayed. UnitPrice is the item's CURRENT price, resolved at read
/// time — carts are not price locks. It only becomes fixed at checkout.
/// </summary>
public class CartLineDto
{
    public Guid MerchItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>False when the item sold out after it was added — checkout will reject it.</summary>
    public bool Available { get; set; } = true;
}

public class CartDto
{
    public List<CartLineDto> Lines { get; set; } = [];
    public int TotalItems { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "PHP";

    /// <summary>True when every line is still purchasable.</summary>
    public bool CanCheckout { get; set; }
}

public class AddToCartRequest
{
    public Guid MerchItemId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Absolute quantity. Zero removes the line.</summary>
public class UpdateCartLineRequest
{
    public Guid MerchItemId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutRequest
{
    public string? Note { get; set; }
}

/// <summary>
/// GCash proof, submitted after checkout to move the order into the queue.
/// The screenshot is what's required; the reference is a convenience.
/// </summary>
public class SubmitReceiptRequest
{
    public string? ScreenshotUrl { get; set; }
    public string? ReferenceNumber { get; set; }
}

/// <summary>Officer bouncing a receipt back to the guilder.</summary>
public class RejectReceiptRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class PaymentReceiptDto
{
    public string? ScreenshotUrl { get; set; }

    /// <summary>Null on receipts where the guilder only sent the screenshot.</summary>
    public string? ReferenceNumber { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
