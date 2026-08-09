using UpcsgWeb.Domain.ValueObjects;
using UpcsgWeb.Shared.Contracts;
using DomainMerchItem = UpcsgWeb.Domain.Merch.MerchItem;

namespace UpcsgWeb.Application.Features.Merch;

internal static class MerchWrites
{
    internal static void Apply(DomainMerchItem item, MerchItemDto req)
    {
        item.UpdateDetails(req.Name, req.Description, Money.Of(req.Price));
        item.ReplacePhotos(req.PhotoUrls);
        item.SetInStock(req.InStock);
        item.SetSale(req.IsOnSale, req.SalePercentage);

        item.SetPreorder(req.IsPreorder, req.IsPreorder ? req.PreorderClosesAt : null);
        item.SetStock(req.Stock);

        ApplyVariants(item, req.Variants);
    }

    private static void ApplyVariants(DomainMerchItem item, List<MerchVariantDto> submitted)
    {
        var keptIds = submitted.Where(v => v.Id != Guid.Empty).Select(v => v.Id).ToHashSet();

        foreach (var existing in item.Variants.Where(v => !keptIds.Contains(v.Id)).ToList())
        {
            item.RemoveVariant(existing.Id);
        }

        foreach (var dto in submitted)
        {
            if (dto.Id == Guid.Empty)
            {
                item.AddVariant(dto.Name, dto.Description, Money.Of(dto.Price), dto.PhotoUrls, dto.Stock);
            }
            else
            {
                item.UpdateVariant(dto.Id, dto.Name, dto.Description, Money.Of(dto.Price), dto.PhotoUrls);
                item.SetVariantStock(dto.Id, dto.Stock);
            }
        }

        var ordered = submitted
            .Where(v => v.Id != Guid.Empty)
            .Select(v => v.Id)
            .ToList();

        if (ordered.Count > 0)
        {
            item.ReorderVariants(ordered);
        }
    }
}
