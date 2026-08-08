namespace UpcsgWeb.Domain.Orders;

public enum OrderStatus
{
    AwaitingPayment,

    Pending,

    Acknowledged,

    Released,

    Received,

    Cancelled,
}
