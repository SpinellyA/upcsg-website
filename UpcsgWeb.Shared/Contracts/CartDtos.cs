namespace UpcsgWeb.Shared.Contracts;

public class CartLineDto
{
    public Guid MerchItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Whether this line can still be ordered at the quantity asked for. Covers the item
    /// being withdrawn, its variant disappearing, a preorder window closing, and stock
    /// running down since it went in the cart.
    /// </summary>
    public bool Available { get; set; } = true;

    /// <summary>
    /// Why the line cannot be ordered, phrased for the guilder. Null while it is fine.
    /// </summary>
    public string? UnavailableReason { get; set; }

    /// <summary>
    /// How many are still to be had. Null for a preorder, which has no ceiling, or when the
    /// item has been withdrawn altogether.
    /// </summary>
    public int? StockLeft { get; set; }
}

public class CartDto
{
    public List<CartLineDto> Lines { get; set; } = [];
    public int TotalItems { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "PHP";

    public bool CanCheckout { get; set; }
}

public class AddToCartRequest
{
    public Guid MerchItemId { get; set; }
    public string? Variant { get; set; }
    public int Quantity { get; set; } = 1;
}

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

public class SubmitReceiptRequest
{
    public string? ScreenshotUrl { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class RejectReceiptRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class PaymentReceiptDto
{
    public string? ScreenshotUrl { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}
