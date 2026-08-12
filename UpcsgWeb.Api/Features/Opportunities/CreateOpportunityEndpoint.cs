using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Opportunities;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Opportunities;

public class CreateOpportunityEndpoint(ISender sender) : Endpoint<OpportunityDto, OpportunityDto>
{
    public override void Configure()
    {
        Post("/opportunities");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(OpportunityDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new CreateOpportunityCommand(req), ct), ct);
}
