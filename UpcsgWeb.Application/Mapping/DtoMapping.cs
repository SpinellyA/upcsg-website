using UpcsgWeb.Domain.Content;
using UpcsgWeb.Domain.Merch;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Domain.Users;
using UpcsgWeb.Shared.Contracts;

using DomainMemberCategory = UpcsgWeb.Domain.Content.MemberCategory;
using WireMemberCategory = UpcsgWeb.Shared.Contracts.MemberCategory;

namespace UpcsgWeb.Application.Mapping;

public static class DtoMapping
{
    public static EventDto ToDto(this GuildEvent e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        StartDateTime = e.StartDateTime,
        EndDateTime = e.EndDateTime,
        Location = e.Location,
        PosterUrl = e.PosterUrl,
    };

    public static MerchItemDto ToDto(this MerchItem m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Description = m.Description,
        Price = m.Price.Amount,
        Stock = m.Stock,
        PhotoUrls = [.. m.PhotoUrls],
        Variants = [.. m.Variants.Select(v => v.ToDto())],
        InStock = m.InStock,
        SalePercentage = m.SalePercentage,
        IsOnSale = m.IsOnSale,
        IsPreorder = m.IsPreorder,
        PreorderClosesAt = m.PreorderClosesAt,

        PriceFrom = m.PriceFrom.Amount,
        ListPriceFrom = m.ListPriceFrom.Amount,
        HasPriceRange = m.HasPriceRange,
        HasActiveSale = m.HasActiveSale,
        IsPreorderClosed = m.IsPreorderClosed,
    };

    public static MerchVariantDto ToDto(this MerchVariant v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Description = v.Description,
        Price = v.Price.Amount,
        Stock = v.Stock,
        PhotoUrls = [.. v.PhotoUrls],
    };

    public static MemberDto ToDto(this Member m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Role = m.Role,
        Category = m.Category == DomainMemberCategory.Faculty
            ? WireMemberCategory.Faculty
            : WireMemberCategory.ExeCom,
        Committee = m.Committee,
        PhotoUrl = m.PhotoUrl,
        Quote = m.Quote,
        Bio = m.Bio,
        DisplayOrder = m.DisplayOrder,
    };

    public static AchievementDto ToDto(this Achievement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Description = a.Description,
        Year = a.Year,
        ImageUrl = a.ImageUrl,
        Category = a.Category,
    };

    public static SiteSettingsDto ToDto(this UpcsgWeb.Domain.Settings.SiteSettings s)
    {
        var (year, month) = s.ResolveEventsMonth();

        return new SiteSettingsDto
        {
            EventsYear = s.EventsYear,
            EventsMonth = s.EventsMonth,
            ResolvedYear = year,
            ResolvedMonth = month,
        };
    }

    public static AppUserDto ToDto(this AppUser u) => new()
    {
        Id = u.Id.ToString(),
        Email = u.Email,
        Name = u.Name,
        PictureUrl = u.PictureUrl,
        Role = u.Role,
    };

    public static OrderDto ToDto(
        this UpcsgWeb.Domain.Orders.Order o,
        AppUser? guilder = null) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        GuilderName = guilder?.Name,
        GuilderEmail = guilder?.Email,
        Status = Enum.Parse<OrderStatusDto>(o.Status.ToString()),
        PlacedAt = o.PlacedAt,
        UpdatedAt = o.UpdatedAt,
        Note = o.Note,
        CancellationReason = o.CancellationReason,
        ReceiptRejectionReason = o.ReceiptRejectionReason,
        Receipt = o.Receipt is null ? null : new PaymentReceiptDto
        {
            ReferenceNumber = o.Receipt.ReferenceNumber,
            ScreenshotUrl = o.Receipt.ScreenshotUrl,
            SubmittedAt = o.Receipt.SubmittedAt,
        },
        Total = o.Total.Amount,
        Currency = o.Total.Currency,

        AmountPaid = o.AmountPaid?.Amount,
        RefundDue = o.RefundDue.Amount,
        FulfilledTotal = o.FulfilledTotal.Amount,
        RefundReference = o.RefundReference,
        RefundSettledAt = o.RefundSettledAt,

        Lines = [.. o.Lines.Select(l => new OrderLineDto
        {
            MerchItemId = l.MerchItemId,
            ItemName = l.ItemName,
            Variant = l.Variant,
            UnitPrice = l.UnitPrice.Amount,
            Quantity = l.Quantity,
            LineTotal = l.LineTotal.Amount,
            Status = Enum.Parse<OrderLineStatusDto>(l.Status.ToString()),
            ShortfallReason = l.ShortfallReason,
        })],
    };
}
