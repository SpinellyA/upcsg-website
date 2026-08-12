using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Opportunities;

namespace UpcsgWeb.Api.Features.Opportunities;

public class DeleteOpportunityEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/opportunities/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new DeleteOpportunityCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
