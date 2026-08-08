using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RefulfilLine;

public record RefulfilLineCommand(Guid OrderId, Guid MerchItemId, string? Variant)
    : ICommand<OrderDto>;
