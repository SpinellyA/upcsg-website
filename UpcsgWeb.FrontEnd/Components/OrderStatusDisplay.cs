using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Components;

public static class OrderStatusDisplay
{
    // Pending means two different things now. For an online order it is the rare case where
    // stock ran out before the reference arrived and an officer has to sort it out. For a cash
    // order it is the ordinary state: waiting for the guilder to come and pay. The overloads
    // that take the whole order say which; the status-only ones stay for callers that have
    // nothing else to go on.

    public static string Label(OrderDto order) =>
        order.AwaitsCashCollection ? "Awaiting cash payment" : Label(order.Status);

    public static string ShortLabel(OrderDto order) =>
        order.AwaitsCashCollection ? "Awaiting cash" : ShortLabel(order.Status);

    public static string OfficerHint(OrderDto order) =>
        order.AwaitsCashCollection
            ? "Take the payment in person, then confirm it here. Nothing is held for this order until you do."
            : OfficerHint(order.Status);

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

    public static string OfficerHint(OrderStatusDto status) => status switch
    {
        OrderStatusDto.AwaitingPayment => "Waiting on the guilder's GCash reference. Nothing to do yet.",
        OrderStatusDto.Pending => "This one needs sorting out by hand: it could not be filled when payment came in.",
        OrderStatusDto.Acknowledged => "Payment confirmed and stock committed. Release the items when they're handed over.",
        OrderStatusDto.Released => "Handed over. Mark received once the guilder confirms.",
        OrderStatusDto.Received => "Complete. Nothing further.",
        OrderStatusDto.Cancelled => "Cancelled. Nothing further.",
        _ => string.Empty,
    };
}
