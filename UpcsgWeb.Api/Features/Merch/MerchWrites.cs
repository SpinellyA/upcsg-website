using UpcsgWeb.Domain.ValueObjects;
using UpcsgWeb.Shared.Contracts;
using DomainMerchItem = UpcsgWeb.Domain.Merch.MerchItem;

namespace UpcsgWeb.Api.Features.Merch;

/// <summary>
/// Applies a submitted item — details, photos and the whole variant list — onto the
/// aggregate. Shared by create and update so the two can't drift.
/// </summary>
public static class MerchWrites
{
    public static void Apply(DomainMerchItem item, MerchItemDto req)
    {
        item.UpdateDetails(req.Name, req.Description, Money.Of(req.Price));
        item.ReplacePhotos(req.PhotoUrls);
        item.SetInStock(req.InStock);

        ApplyVariants(item, req.Variants);
    }

    /// <summary>
    /// The CMS edits the variant list as a whole and saves once, so this reconciles rather
    /// than exposing per-variant endpoints: id 0 is new, a known id is an edit, and anything
    /// the form no longer carries has been deleted.
    ///
    /// Deliberately not "clear and re-add": that would issue new ids on every save and
    /// churn the rows behind cart lines for no reason.
    /// </summary>
    private static void ApplyVariants(DomainMerchItem item, List<MerchVariantDto> submitted)
    {
        var keptIds = submitted.Where(v => v.Id != 0).Select(v => v.Id).ToHashSet();

        foreach (var existing in item.Variants.Where(v => !keptIds.Contains(v.Id)).ToList())
        {
            item.RemoveVariant(existing.Id);
        }

        foreach (var dto in submitted)
        {
            if (dto.Id == 0)
            {
                item.AddVariant(dto.Name, dto.Description, Money.Of(dto.Price), dto.PhotoUrls);
            }
            else
            {
                item.UpdateVariant(dto.Id, dto.Name, dto.Description, Money.Of(dto.Price), dto.PhotoUrls);
            }
        }

        // The order the officer arranged them in is the order guilders see.
        var ordered = submitted
            .Where(v => v.Id != 0)
            .Select(v => v.Id)
            .ToList();

        if (ordered.Count > 0)
        {
            item.ReorderVariants(ordered);
        }
    }
}
