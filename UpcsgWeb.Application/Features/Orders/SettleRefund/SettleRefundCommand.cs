using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SettleRefund;

public record SettleRefundCommand(Guid OrderId, string Reference) : ICommand<OrderDto>;
