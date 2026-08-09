using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Events;

namespace UpcsgWeb.Api.Features.Events;

public class DeleteEventEndpoint(ISender sender) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/events/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await sender.Send(new DeleteEventCommand(Route<Guid>("id")), ct);
        await Send.NoContentAsync(ct);
    }
}
