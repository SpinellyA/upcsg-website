using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Api.Features.Carts;

internal static class CartOps
{
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
