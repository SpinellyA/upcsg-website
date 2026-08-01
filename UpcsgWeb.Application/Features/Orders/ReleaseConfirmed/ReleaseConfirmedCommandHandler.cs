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
        // ForUpdate, not the AsNoTracking read: the plain query returns detached orders,
        // so Release() would mutate throwaway objects and report success having saved
        // nothing.
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
                // One awkward order must not stop the rest of the queue. This is the one
                // place a DomainException is caught rather than surfaced, because the
                // whole point of the batch is that it keeps going. Say which ones, so the
                // officer can go and look rather than wonder.
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
