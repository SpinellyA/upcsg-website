using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class CreateEventEndpoint(IEventRepository events, IUnitOfWork uow)
    : Endpoint<EventDto, EventDto>
{
    public override void Configure()
    {
        Post("/events");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(EventDto req, CancellationToken ct)
    {
        GuildEvent created;
        try
        {
            created = GuildEvent.Create(
                req.Title, req.Description, req.StartDateTime, req.EndDateTime, req.Location, req.PosterUrl);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        events.Add(created);
        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(created.ToDto(), ct);
    }
}
