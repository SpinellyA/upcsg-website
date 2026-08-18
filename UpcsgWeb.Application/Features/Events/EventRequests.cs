using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Events;

public record GetEventQuery(Guid Id) : IQuery<EventDto?>;

public record ListEventsForMonthQuery(int Year, int Month) : IQuery<List<EventDto>>;

public record ListComingSoonEventsQuery : IQuery<List<EventDto>>;

public record CreateEventCommand(EventDto Event) : ICommand<EventDto>;

public record UpdateEventCommand(Guid Id, EventDto Event) : ICommand<EventDto>;

public record DeleteEventCommand(Guid Id) : ICommand;
