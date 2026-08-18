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
        Summary(s => s.Summary =
            "Opportunities, soonest deadline first. Includes ones that have already closed, "
            + "which the site lists separately under Past; callers wanting only live entries "
            + "should filter on isClosed.");
    }

    // Open and closed in one response rather than a second endpoint for the archive: the
    // page renders both lists at once, and the roster is small enough that splitting it
    // would cost a round trip to save very little payload.
    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListOpportunitiesQuery(OpenOnly: false), ct), ct);
}
