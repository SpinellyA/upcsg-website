using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Domain.Orders;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SubmitReceipt;

public class SubmitReceiptCommandHandler(IUnitOfWork uow)
    : ICommandHandler<SubmitReceiptCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        SubmitReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var order = await uow.Orders.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundException("That order");

        if (order.UserId != command.CallerId)
        {
            throw new ForbiddenException("That order is not yours.");
        }

        order.SubmitReceipt(
            PaymentReceipt.FromScreenshot(command.ScreenshotUrl, command.ReferenceNumber));

        // Confirmed on the spot, because there is no payment provider to verify the reference
        // against and leaving guilders queued behind a review the guild cannot perform helps
        // nobody. This call is the whole of that policy: delete it once a payment API exists
        // and the order simply waits in Pending for a real check instead.
        var items = await uow.Merch.GetManyAsync(
            order.Lines.Select(l => l.MerchItemId), cancellationToken);

        order.ConfirmOnlinePaymentUnchecked(items.ToDictionary(i => i.Id));

        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
