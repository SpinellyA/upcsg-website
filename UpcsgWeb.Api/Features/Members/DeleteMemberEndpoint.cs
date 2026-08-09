using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Members;

namespace UpcsgWeb.Api.Features.Members;

public class DeleteMemberEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/members/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new DeleteMemberCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
