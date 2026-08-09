using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class ListMerchEndpoint(ISender sender) : EndpointWithoutRequest<List<MerchItemDto>>
{
    public override void Configure()
    {
        Get("/merch");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListMerchQuery(), ct), ct);
}
