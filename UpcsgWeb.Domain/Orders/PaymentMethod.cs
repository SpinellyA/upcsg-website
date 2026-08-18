namespace UpcsgWeb.Domain.Orders;

public enum PaymentMethod
{
    /// <summary>
    /// Handed over in person and recorded by an officer. Nothing is committed until that
    /// happens, so a cash order holds no stock while it waits, which is what puts it behind
    /// online payment when the two want the same last item.
    /// </summary>
    Cash,

    /// <summary>
    /// Paid online, then the reference is submitted. There is no payment provider wired up to
    /// verify it, so the guild takes the reference at face value and confirms immediately;
    /// officers cancel the ones that turn out to be bad.
    /// </summary>
    GCash,
}
