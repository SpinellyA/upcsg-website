namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// Lifecycle of a merch order. Stored by name, not ordinal, so inserting a stage later
/// can't silently reinterpret existing rows.
///
/// Checkout → receipt → officer handling:
///   AwaitingPayment → Pending → Acknowledged → Released → Received
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Checked out, but no GCash receipt yet. The order exists so the guilder has
    /// something to pay against, but no officer should be looking at it.
    /// </summary>
    AwaitingPayment,

    /// <summary>Receipt submitted; waiting for an officer to verify payment.</summary>
    Pending,

    /// <summary>An officer confirmed the payment and the stock.</summary>
    Acknowledged,

    /// <summary>Handed over for pickup / dispatched.</summary>
    Released,

    /// <summary>Guilder confirmed they have it. Terminal.</summary>
    Received,

    /// <summary>
    /// Called off before release. Terminal.
    /// Beyond the stages you listed — an unpaid checkout otherwise sits in
    /// AwaitingPayment forever with nothing able to close it.
    /// </summary>
    Cancelled,
}
