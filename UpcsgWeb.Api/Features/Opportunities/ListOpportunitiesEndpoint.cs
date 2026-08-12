using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Opportunities;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Opportunities;

public class ListOpportunitiesEndpoint(ISender sender) : EndpointWithoutRequest<List<OpportunityDto>>
{
    public override void Configure()
    {
        Get("/opportunities");
        AllowAnonymous();
        Summary(s => s.Summary = "Opportunities still open, soonest deadline first.");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListOpportunitiesQuery(OpenOnly: true), ct), ct);
}
