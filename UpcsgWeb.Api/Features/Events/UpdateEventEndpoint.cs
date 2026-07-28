using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Events;

public class UpdateEventEndpoint(IEventRepository events, IUnitOfWork uow)
    : Endpoint<EventDto, EventDto>
{
    public override void Configure()
    {
        Put("/events/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(EventDto req, CancellationToken ct)
    {
        // Route id wins over the body, so a mismatched payload can't retarget the write.
        var existing = await events.GetByIdAsync(Route<int>("id"), ct);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            existing.Update(
                req.Title, req.Description, req.StartDateTime, req.EndDateTime, req.Location, req.PosterUrl);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(existing.ToDto(), ct);
    }
}
