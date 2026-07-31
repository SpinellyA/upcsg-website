namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// Whether a line will actually be handed over.
///
/// Separate from the order's own status on purpose. An order can be Acknowledged and
/// moving toward Released while one of its lines is owed back as money — collapsing the
/// two would mean either the whole order stalls for one missing size, or the shortfall
/// disappears from view.
/// </summary>
public enum OrderLineStatus
{
    /// <summary>Stock was taken; the guilder gets this.</summary>
    ToFulfil = 0,

    /// <summary>Could not be filled. The money for it is owed back.</summary>
    RefundDue = 1,

    /// <summary>The money has been sent back. Terminal.</summary>
    Refunded = 2,
}
