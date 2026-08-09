using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IQuery<AppUserDto>;
