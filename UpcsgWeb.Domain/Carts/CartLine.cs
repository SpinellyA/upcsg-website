using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Carts;

public class CartLine : Entity
{
    private CartLine() { }

    internal CartLine(Guid merchItemId, string? variant, int quantity)
    {
        Id = Guid.CreateVersion7();
        MerchItemId = merchItemId;
        Variant = variant;
        Quantity = quantity;
    }

    public Guid CartId { get; private set; }
    public Guid MerchItemId { get; private set; }
    public string? Variant { get; private set; }
    public int Quantity { get; private set; }

    internal void SetQuantity(int quantity) => Quantity = quantity;
}
