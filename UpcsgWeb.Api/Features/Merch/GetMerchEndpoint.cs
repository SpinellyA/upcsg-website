using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class GetMerchEndpoint(ISender sender) : EndpointWithoutRequest<MerchItemDto>
{
    public override void Configure()
    {
        Get("/merch/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await sender.Send(new GetMerchItemQuery(Route<Guid>("id")), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found, ct);
    }
}
