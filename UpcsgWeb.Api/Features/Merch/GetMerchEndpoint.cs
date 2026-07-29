using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

/// <summary>
/// Backs the merch detail page. Fetching by id rather than filtering the catalogue keeps
/// a shared product link working as the store grows.
/// </summary>
public class GetMerchEndpoint(IMerchRepository merch) : EndpointWithoutRequest<MerchItemDto>
{
    public override void Configure()
    {
        Get("/merch/{id:int}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await merch.GetByIdAsync(Route<int>("id"), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found.ToDto(), ct);
    }
}
