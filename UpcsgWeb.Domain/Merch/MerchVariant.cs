using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;

namespace UpcsgWeb.Domain.Merch;

public class MerchVariant : Entity
{
    private readonly List<string> _photoUrls = [];

    private MerchVariant() { }

    public Guid MerchItemId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Money Price { get; private set; } = Money.Zero();

    public IReadOnlyList<string> PhotoUrls => _photoUrls.AsReadOnly();

    public int DisplayOrder { get; private set; }

    public int Stock { get; private set; }

    internal static MerchVariant Create(string name, string description, Money price, int displayOrder)
    {
        var variant = new MerchVariant { Id = Guid.CreateVersion7() };
        variant.Update(name, description, price);
        variant.DisplayOrder = displayOrder;
        return variant;
    }

    internal void SetStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        Stock = stock;
    }

    internal void Deduct(int quantity)
    {
        if (quantity > Stock)
        {
            throw new DomainException($"Only {Stock} of '{Name}' left.");
        }

        Stock -= quantity;
    }

    internal void Restock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Restock quantity must be at least 1.");
        }

        Stock += quantity;
    }

    internal void Update(string name, string description, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A variant needs a name.");
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
    }

    internal void SetDisplayOrder(int order) => DisplayOrder = order;

    internal void ReplacePhotos(IEnumerable<string> photoUrls)
    {
        _photoUrls.Clear();

        foreach (var url in photoUrls ?? [])
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                _photoUrls.Add(url.Trim());
            }
        }
    }

    public bool NameMatches(string? candidate) =>
        candidate is not null && string.Equals(Name, candidate.Trim(), StringComparison.OrdinalIgnoreCase);
}
