using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Merch;

namespace UpcsgWeb.Api.Features.Merch;

public class DeleteMerchEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/merch/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new DeleteMerchCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
