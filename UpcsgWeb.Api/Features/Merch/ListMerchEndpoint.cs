using FastEndpoints;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class ListMerchEndpoint(IMerchRepository merch) : EndpointWithoutRequest<List<MerchItemDto>>
{
    public override void Configure()
    {
        Get("/merch");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Sold-out items stay in the list; the store shows them greyed rather than
        // hiding what exists.
        var items = await merch.GetAllAsync(ct);
        await Send.OkAsync([.. items.Select(m => m.ToDto())], ct);
    }
}
