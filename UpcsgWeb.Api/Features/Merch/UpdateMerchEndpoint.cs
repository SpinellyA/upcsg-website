using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.ValueObjects;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class UpdateMerchEndpoint(IMerchRepository merch, IUnitOfWork uow)
    : Endpoint<MerchItemDto, MerchItemDto>
{
    public override void Configure()
    {
        Put("/merch/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MerchItemDto req, CancellationToken ct)
    {
        var item = await merch.GetByIdAsync(Route<int>("id"), ct);
        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            // Repricing is safe: existing order lines hold their own snapshots.
            item.UpdateDetails(req.Name, req.Description, Money.Of(req.Price), req.ImageUrl);
            item.ReplaceVariants(req.Variants);
            item.SetStock(req.InStock);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(item.ToDto(), ct);
    }
}
