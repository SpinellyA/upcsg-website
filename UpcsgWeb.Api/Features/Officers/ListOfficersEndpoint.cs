using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Officers.ListOfficers;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Officers;

public class ListOfficersEndpoint(ISender sender) : EndpointWithoutRequest<List<OfficerEmailDto>>
{
    public override void Configure()
    {
        Get("/admin/officers");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Addresses that get officer rights (officers only).");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListOfficersQuery(), ct), ct);
}
