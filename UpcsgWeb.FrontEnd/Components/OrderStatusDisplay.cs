using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Components;

/// <summary>
/// One place that turns an order status into words and a CSS slug, so the table, the
/// pills, the chain and the member pages can never disagree about what a stage is called.
/// </summary>
public static class OrderStatusDisplay
{
    public static string Label(OrderStatusDto status) => status switch
    {
        OrderStatusDto.AwaitingPayment => "Awaiting payment",
        OrderStatusDto.Pending => "Verifying payment",
        OrderStatusDto.Acknowledged => "Confirmed",
        OrderStatusDto.Released => "Released",
        OrderStatusDto.Received => "Received",
        OrderStatusDto.Cancelled => "Cancelled",
        _ => status.ToString(),
    };

    /// <summary>Short label for tight spaces like the member-facing trail.</summary>
    public static string ShortLabel(OrderStatusDto status) => status switch
    {
        OrderStatusDto.AwaitingPayment => "Awaiting payment",
        OrderStatusDto.Pending => "Verifying",
        OrderStatusDto.Acknowledged => "Confirmed",
        OrderStatusDto.Released => "Released",
        OrderStatusDto.Received => "Received",
        OrderStatusDto.Cancelled => "Cancelled",
        _ => status.ToString(),
    };

    public static string Slug(OrderStatusDto status) => status switch
    {
        OrderStatusDto.AwaitingPayment => "awaiting",
        OrderStatusDto.Pending => "pending",
        OrderStatusDto.Acknowledged => "confirmed",
        OrderStatusDto.Released => "released",
        OrderStatusDto.Received => "received",
        OrderStatusDto.Cancelled => "cancelled",
        _ => "awaiting",
    };

    /// <summary>What the officer is waiting on, phrased as the next action.</summary>
    public static string OfficerHint(OrderStatusDto status) => status switch
    {
        OrderStatusDto.AwaitingPayment => "Waiting on the guilder's GCash receipt. Nothing to do yet.",
        OrderStatusDto.Pending => "Check the receipt against the total, then confirm or reject it.",
        OrderStatusDto.Acknowledged => "Payment confirmed. Release the items when they're handed over.",
        OrderStatusDto.Released => "Handed over. Mark received once the guilder confirms.",
        OrderStatusDto.Received => "Complete. Nothing further.",
        OrderStatusDto.Cancelled => "Cancelled. Nothing further.",
        _ => string.Empty,
    };
}
