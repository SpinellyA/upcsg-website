using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Carts;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Application.Features.Carts;

internal static class CartPricing
{
    internal static async Task<IReadOnlyDictionary<Guid, MerchItem>> LoadPricingAsync(
        this IUnitOfWork uow, Cart cart, CancellationToken ct)
    {
        if (cart.IsEmpty)
        {
            return new Dictionary<Guid, MerchItem>();
        }

        var items = await uow.Merch.GetManyAsync(cart.Lines.Select(l => l.MerchItemId), ct);
        return items.ToDictionary(i => i.Id);
    }
}
