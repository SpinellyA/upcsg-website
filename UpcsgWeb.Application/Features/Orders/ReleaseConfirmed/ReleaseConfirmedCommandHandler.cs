using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.ReleaseConfirmed;

public class ReleaseConfirmedCommandHandler(IUnitOfWork uow)
    : ICommandHandler<ReleaseConfirmedCommand, ReleaseConfirmedDto>
{
    public async Task<ReleaseConfirmedDto> Handle(
        ReleaseConfirmedCommand command,
        CancellationToken cancellationToken)
    {
        var confirmed = await uow.Orders.GetByStatusForUpdateAsync(
            OrderStatus.Acknowledged, cancellationToken);

        var released = new List<Guid>();
        var skipped = new List<string>();

        foreach (var order in confirmed)
        {
            try
            {
                order.Release();
                released.Add(order.Id);
            }
            catch (DomainException ex)
            {
                skipped.Add($"#{order.Id}: {ex.Message}");
            }
        }

        await uow.SaveChangesAsync(cancellationToken);

        return new ReleaseConfirmedDto
        {
            ReleasedCount = released.Count,
            ReleasedOrderIds = released,
            Skipped = skipped,
        };
    }
}
