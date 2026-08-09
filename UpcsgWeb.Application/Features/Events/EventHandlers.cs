using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Events;

public class GetEventQueryHandler(IUnitOfWork uow) : IQueryHandler<GetEventQuery, EventDto?>
{
    public async Task<EventDto?> Handle(GetEventQuery query, CancellationToken ct)
    {
        var found = await uow.Events.GetByIdAsync(query.Id, ct);
        return found?.ToDto();
    }
}

public class ListEventsForMonthQueryHandler(IUnitOfWork uow)
    : IQueryHandler<ListEventsForMonthQuery, List<EventDto>>
{
    public async Task<List<EventDto>> Handle(ListEventsForMonthQuery query, CancellationToken ct)
    {
        var result = await uow.Events.GetForMonthAsync(query.Year, query.Month, ct);
        return [.. result.Select(e => e.ToDto())];
    }
}

public class CreateEventCommandHandler(IUnitOfWork uow) : ICommandHandler<CreateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CreateEventCommand command, CancellationToken ct)
    {
        var dto = command.Event;

        var created = GuildEvent.Create(
            dto.Title, dto.Description, dto.StartDateTime, dto.EndDateTime, dto.Location, dto.PosterUrl);

        uow.Events.Add(created);
        await uow.SaveChangesAsync(ct);

        return created.ToDto();
    }
}

public class UpdateEventCommandHandler(IUnitOfWork uow) : ICommandHandler<UpdateEventCommand, EventDto>
{
    public async Task<EventDto> Handle(UpdateEventCommand command, CancellationToken ct)
    {
        var existing = await uow.Events.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That event");

        var dto = command.Event;
        existing.Update(
            dto.Title, dto.Description, dto.StartDateTime, dto.EndDateTime, dto.Location, dto.PosterUrl);

        await uow.SaveChangesAsync(ct);

        return existing.ToDto();
    }
}

public class DeleteEventCommandHandler(IUnitOfWork uow) : ICommandHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand command, CancellationToken ct)
    {
        var existing = await uow.Events.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That event");

        uow.Events.Remove(existing);
        await uow.SaveChangesAsync(ct);
    }
}
