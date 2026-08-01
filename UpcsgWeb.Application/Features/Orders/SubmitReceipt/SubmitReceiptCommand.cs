using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SubmitReceipt;

/// <summary>Guilder attaches GCash proof, moving their order into the officers' queue.</summary>
public record SubmitReceiptCommand(
    Guid OrderId,
    Guid CallerId,
    string? ScreenshotUrl,
    string? ReferenceNumber) : ICommand<OrderDto>;
