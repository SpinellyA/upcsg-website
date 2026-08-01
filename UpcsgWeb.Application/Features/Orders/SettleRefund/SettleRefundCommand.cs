using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SettleRefund;

/// <summary>
/// Records that money owed on an order actually went back, with the GCash reference.
///
/// The transfer itself happens in GCash — there is no payment API here. What this does is
/// make the transfer auditable: without it, a partial refund is an officer's private act
/// that the Treasurer cannot reconcile and the next ExeCom cannot explain.
/// </summary>
public record SettleRefundCommand(Guid OrderId, string Reference) : ICommand<OrderDto>;
