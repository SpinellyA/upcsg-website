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
        var items = await merch.GetAllAsync(ct);
        await Send.OkAsync([.. items.Select(m => m.ToDto())], ct);
    }
}
