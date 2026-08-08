using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RejectReceipt;

public record RejectReceiptCommand(Guid OrderId, string Reason) : ICommand<OrderDto>;
