using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.RefulfilLine;

/// <summary>
/// Fills a line that previously fell short, now that stock exists again.
///
/// Deliberately officer-initiated rather than something a restock does automatically: the
/// guilder may already have been told they are being refunded, and quietly resurrecting
/// their order after that conversation is worse than asking.
/// </summary>
public record RefulfilLineCommand(Guid OrderId, Guid MerchItemId, string? Variant)
    : ICommand<OrderDto>;
