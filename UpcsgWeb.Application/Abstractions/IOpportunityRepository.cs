using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Application.Abstractions;

public interface IOpportunityRepository : IRepository<Opportunity>
{
    Task<IReadOnlyList<Opportunity>> GetOpenAsync(CancellationToken ct = default);
}
