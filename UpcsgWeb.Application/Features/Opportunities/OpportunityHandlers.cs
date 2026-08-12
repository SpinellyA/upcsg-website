using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Content;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Opportunities;

public class GetOpportunityQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetOpportunityQuery, OpportunityDto?>
{
    public async Task<OpportunityDto?> Handle(GetOpportunityQuery query, CancellationToken ct)
    {
        var found = await uow.Opportunities.GetByIdAsync(query.Id, ct);
        return found?.ToDto();
    }
}

public class ListOpportunitiesQueryHandler(IUnitOfWork uow)
    : IQueryHandler<ListOpportunitiesQuery, List<OpportunityDto>>
{
    public async Task<List<OpportunityDto>> Handle(ListOpportunitiesQuery query, CancellationToken ct)
    {
        var found = query.OpenOnly
            ? await uow.Opportunities.GetOpenAsync(ct)
            : await uow.Opportunities.GetAllAsync(ct);

        return [.. found.Select(o => o.ToDto())];
    }
}

public class CreateOpportunityCommandHandler(IUnitOfWork uow)
    : ICommandHandler<CreateOpportunityCommand, OpportunityDto>
{
    public async Task<OpportunityDto> Handle(CreateOpportunityCommand command, CancellationToken ct)
    {
        var dto = command.Opportunity;

        var opportunity = Opportunity.Create(
            dto.Title,
            dto.Description,
            (OpportunityKind)dto.Kind,
            dto.Organiser,
            dto.Location,
            dto.OpensAt?.UtcDateTime,
            dto.ClosesAt?.UtcDateTime,
            dto.HappensAt?.UtcDateTime,
            dto.Url,
            dto.PosterUrl);

        opportunity.Feature(dto.IsFeatured);

        uow.Opportunities.Add(opportunity);
        await uow.SaveChangesAsync(ct);

        return opportunity.ToDto();
    }
}

public class UpdateOpportunityCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateOpportunityCommand, OpportunityDto>
{
    public async Task<OpportunityDto> Handle(UpdateOpportunityCommand command, CancellationToken ct)
    {
        var opportunity = await uow.Opportunities.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That opportunity");

        var dto = command.Opportunity;

        opportunity.Update(
            dto.Title,
            dto.Description,
            (OpportunityKind)dto.Kind,
            dto.Organiser,
            dto.Location,
            dto.OpensAt?.UtcDateTime,
            dto.ClosesAt?.UtcDateTime,
            dto.HappensAt?.UtcDateTime,
            dto.Url,
            dto.PosterUrl);

        opportunity.Feature(dto.IsFeatured);

        await uow.SaveChangesAsync(ct);

        return opportunity.ToDto();
    }
}

public class DeleteOpportunityCommandHandler(IUnitOfWork uow)
    : ICommandHandler<DeleteOpportunityCommand>
{
    public async Task Handle(DeleteOpportunityCommand command, CancellationToken ct)
    {
        var opportunity = await uow.Opportunities.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That opportunity");

        uow.Opportunities.Remove(opportunity);
        await uow.SaveChangesAsync(ct);
    }
}
