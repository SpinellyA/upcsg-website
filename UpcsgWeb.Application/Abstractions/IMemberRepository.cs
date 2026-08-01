using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Application.Abstractions;

/// <summary>
/// Nothing beyond the generic CRUD: the About page orders the roster itself, and
/// <see cref="IRepository{T}.GetAllAsync"/> already returns it sorted.
/// </summary>
public interface IMemberRepository : IRepository<Member>;
