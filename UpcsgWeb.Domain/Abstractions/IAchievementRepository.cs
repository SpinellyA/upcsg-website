using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Domain.Abstractions;

/// <summary>Plain CRUD; the Hall of Fame page does its own grouping and filtering.</summary>
public interface IAchievementRepository : IRepository<Achievement>;
