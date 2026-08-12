using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Opportunities;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Opportunities;

public class UpdateOpportunityEndpoint(ISender sender) : Endpoint<OpportunityDto, OpportunityDto>
{
    public override void Configure()
    {
        Put("/opportunities/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(OpportunityDto req, CancellationToken ct) =>
        await Send.OkAsync(
            await sender.Send(new UpdateOpportunityCommand(Route<Guid>("id"), req), ct), ct);
}
