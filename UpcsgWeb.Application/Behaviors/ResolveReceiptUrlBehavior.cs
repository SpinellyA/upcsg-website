using MediatR;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Behaviors;

/// <summary>
/// Turns the stored receipt reference into something a browser can actually load.
///
/// A receipt lives in the private bucket, so what the order records is a storage key, not
/// a URL. Every response carrying an order has to swap that key for a short-lived
/// presigned GET, or the officer sees a broken image.
///
/// Done as a pipeline behavior rather than in each handler on purpose: nine handlers
/// already return an OrderDto — checkout, submit, reject, acknowledge, release, settle,
/// refulfil and the two queries — and the failure mode for missing one is a broken image
/// on a screen nobody looks at until an order is disputed. Here it is applied once and
/// the tenth handler gets it for free.
///
/// Public objects pass through unchanged, as do receipts recorded as full URLs before the
/// private bucket existed; see IMediaStore.CreateReadUrlAsync.
/// </summary>
public class ResolveReceiptUrlBehavior<TRequest, TResponse>(IMediaStore media)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        switch (response)
        {
            case OrderDto order:
                await ResolveAsync(order, cancellationToken);
                break;

            case IEnumerable<OrderDto> orders:
                foreach (var each in orders)
                {
                    await ResolveAsync(each, cancellationToken);
                }

                break;
        }

        return response;
    }

    private async Task ResolveAsync(OrderDto order, CancellationToken ct)
    {
        if (order.Receipt is null || string.IsNullOrWhiteSpace(order.Receipt.ScreenshotUrl))
        {
            return;
        }

        // Overwritten rather than carried in a second field: every caller already reads
        // ScreenshotUrl, and the name still describes what it holds — the URL that shows
        // the screenshot. That it is now temporary is the storage layer's business.
        order.Receipt.ScreenshotUrl =
            await media.CreateReadUrlAsync(order.Receipt.ScreenshotUrl, ct);
    }
}
