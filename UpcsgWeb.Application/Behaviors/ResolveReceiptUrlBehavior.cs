using MediatR;
using UpcsgWeb.Domain.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Behaviors;

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

        order.Receipt.ScreenshotUrl =
            await media.CreateReadUrlAsync(order.Receipt.ScreenshotUrl, ct);
    }
}
