using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RejectReceipt;

/// <summary>Officer sends a receipt back so the guilder can resubmit.</summary>
public record RejectReceiptCommand(Guid OrderId, string Reason) : ICommand<OrderDto>;
