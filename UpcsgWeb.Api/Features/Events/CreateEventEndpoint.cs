using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Events;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class CreateEventEndpoint(ISender sender) : Endpoint<EventDto, EventDto>
{
    public override void Configure()
    {
        Post("/events");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(EventDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new CreateEventCommand(req), ct), ct);
}
