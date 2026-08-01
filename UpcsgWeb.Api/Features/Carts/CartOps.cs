using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Api.Features.Carts;

/// <summary>
/// Shared cart plumbing. A static helper rather than a base class, because every
/// endpoint already derives from a FastEndpoints type.
///
/// Every cart endpoint resolves the cart from the JWT — none accepts a userId from the
/// caller, or one guilder could read and edit another's cart.
/// </summary>
internal static class CartOps
{
    /// <summary>Loads the guilder's cart, creating an empty one on first use.</summary>
    public static async Task<Cart> GetOrCreateAsync(
        ICartRepository carts, Guid userId, CancellationToken ct)
    {
        var existing = await carts.GetForUserAsync(userId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var created = Cart.Create(userId);
        carts.Add(created);
        return created;
    }

    /// <summary>Resolves every merch item the cart references in a single query.</summary>
    public static async Task<IReadOnlyDictionary<Guid, MerchItem>> ResolveItemsAsync(
        Cart cart, IMerchRepository merch, CancellationToken ct)
    {
        if (cart.IsEmpty)
        {
            return new Dictionary<Guid, MerchItem>();
        }

        var items = await merch.GetManyAsync(cart.Lines.Select(l => l.MerchItemId), ct);
        return items.ToDictionary(i => i.Id);
    }
}
