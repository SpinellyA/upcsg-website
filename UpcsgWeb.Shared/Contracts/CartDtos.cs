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

    public bool Available { get; set; } = true;
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
