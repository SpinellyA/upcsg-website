using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Officers.AddOfficer;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Officers;

/// <summary>
/// Adds an address to the officer allowlist.
///
/// This is the one endpoint that can hand out administrative rights, so it is officers
/// only and there is deliberately no self-service route to it.
/// </summary>
public class AddOfficerEndpoint(ISender sender) : Endpoint<AddOfficerRequest, OfficerEmailDto>
{
    public override void Configure()
    {
        Post("/admin/officers");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Grant officer rights to an email address (officers only).");
    }

    public override async Task HandleAsync(AddOfficerRequest req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new AddOfficerCommand(req.Email, req.Note), ct), ct);
}
