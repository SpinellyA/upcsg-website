using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;
using UpcsgWeb.Shared.Contracts;
using DomainMerchItem = UpcsgWeb.Domain.Merch.MerchItem;

namespace UpcsgWeb.Api.Features.Merch;

public class CreateMerchEndpoint(IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<MerchItemDto, MerchItemDto>
{
    public override void Configure()
    {
        Post("/merch");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MerchItemDto req, CancellationToken ct)
    {
        DomainMerchItem item;
        try
        {
            item = DomainMerchItem.Create(
                req.Name, req.Description, Money.Of(req.Price), req.Variants, req.ImageUrl);

            item.SetStock(req.InStock);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        merch.Add(item);
        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(item.ToDto(), ct);
    }
}
