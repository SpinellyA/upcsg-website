using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.GetOrder;

public class GetOrderQueryHandler(IUnitOfWork uow) : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(query.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        if (order.UserId != query.CallerId && !query.CallerIsOfficer)
        {
            throw new ForbiddenException("That order is not yours.");
        }

        var guilder = query.CallerIsOfficer
            ? await uow.Users.GetByIdAsync(order.UserId, cancellationToken)
            : null;

        return order.ToDto(guilder);
    }
}
