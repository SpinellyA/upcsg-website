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

        // Paying is the guilder's own act. Officers can move statuses but must not be
        // able to fabricate a receipt on someone's behalf, so there is no admin bypass.
        if (order.UserId != command.CallerId)
        {
            throw new ForbiddenException("That order is not yours.");
        }

        order.SubmitReceipt(
            PaymentReceipt.FromScreenshot(command.ScreenshotUrl, command.ReferenceNumber));

        await uow.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
