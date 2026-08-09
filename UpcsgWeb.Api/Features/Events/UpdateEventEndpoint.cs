using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class UpdateEventEndpoint(ISender sender) : Endpoint<EventDto, EventDto>
{
    public override void Configure()
    {
        Put("/events/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(EventDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new UpdateEventCommand(Route<Guid>("id"), req), ct), ct);
}
