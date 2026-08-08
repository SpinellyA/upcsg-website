using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Application.Abstractions;

public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    void Add(T entity);

    void Remove(T entity);
}
