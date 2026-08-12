using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Opportunities;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Opportunities;

public class AdminListOpportunitiesEndpoint(ISender sender)
    : EndpointWithoutRequest<List<OpportunityDto>>
{
    public override void Configure()
    {
        Get("/admin/opportunities");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Every opportunity, including ones that have closed.");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListOpportunitiesQuery(OpenOnly: false), ct), ct);
}
