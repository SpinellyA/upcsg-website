using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Orders.SubmitReceipt;

public record SubmitReceiptCommand(
    Guid OrderId,
    Guid CallerId,
    string? ScreenshotUrl,
    string? ReferenceNumber) : ICommand<OrderDto>;
