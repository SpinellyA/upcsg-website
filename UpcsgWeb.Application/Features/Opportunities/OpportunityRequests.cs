using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Opportunities;

public record GetOpportunityQuery(Guid Id) : IQuery<OpportunityDto?>;

public record ListOpportunitiesQuery(bool OpenOnly) : IQuery<List<OpportunityDto>>;

public record CreateOpportunityCommand(OpportunityDto Opportunity) : ICommand<OpportunityDto>;

public record UpdateOpportunityCommand(Guid Id, OpportunityDto Opportunity) : ICommand<OpportunityDto>;

public record DeleteOpportunityCommand(Guid Id) : ICommand;
