using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Officers.RemoveOfficer;

namespace UpcsgWeb.Api.Features.Officers;

/// <summary>Removes an address from the allowlist and demotes its account.</summary>
public class RemoveOfficerEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/admin/officers/{id:guid}");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Revoke officer rights from an address (officers only).");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new RemoveOfficerCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
