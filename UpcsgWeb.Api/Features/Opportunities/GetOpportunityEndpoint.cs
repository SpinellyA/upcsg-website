using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Opportunities;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Opportunities;

public class GetOpportunityEndpoint(ISender sender) : EndpointWithoutRequest<OpportunityDto>
{
    public override void Configure()
    {
        Get("/opportunities/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await sender.Send(new GetOpportunityQuery(Route<Guid>("id")), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found, ct);
    }
}
