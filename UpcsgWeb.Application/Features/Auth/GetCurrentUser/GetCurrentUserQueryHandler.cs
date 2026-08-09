using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Auth.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetCurrentUserQuery, AppUserDto>
{
    public async Task<AppUserDto> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        var user = await uow.Users.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new UnauthorizedException("That account no longer exists.");

        return user.ToDto();
    }
}
