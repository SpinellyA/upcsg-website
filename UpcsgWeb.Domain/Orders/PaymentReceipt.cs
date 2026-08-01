using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Orders;

/// <summary>
/// Proof of a GCash transfer, supplied by the guilder after checkout.
///
/// This is evidence, not verification — an officer still confirms the money arrived.
/// The model deliberately does not treat a submitted receipt as a paid order, which is
/// exactly why Pending means "an officer needs to look at this".
/// </summary>
public sealed record PaymentReceipt
{
    public const int MaxReferenceLength = 50;

    /// <summary>
    /// The screenshot is the proof. It carries the amount, the timestamp and the
    /// recipient, all of which an officer can check against the guild's GCash log; a
    /// typed reference carries none of that and is easy to mistype or invent.
    /// </summary>
    public string ScreenshotUrl { get; }

    /// <summary>
    /// Optional. Useful for searching the transaction log, but the screenshot already
    /// shows it, so making a guilder retype it only adds a way to get it wrong.
    /// </summary>
    public string? ReferenceNumber { get; }

    public DateTime SubmittedAt { get; }

    private PaymentReceipt(string screenshotUrl, string? referenceNumber, DateTime submittedAt)
    {
        ScreenshotUrl = screenshotUrl;
        ReferenceNumber = referenceNumber;
        SubmittedAt = submittedAt;
    }

    /// <summary>
    /// Deliberately not called Submit any more: the arguments swapped places, and a
    /// rename makes every old call site fail to compile instead of quietly meaning the
    /// opposite of what it says.
    /// </summary>
    public static PaymentReceipt FromScreenshot(string? screenshotUrl, string? referenceNumber = null)
    {
        if (string.IsNullOrWhiteSpace(screenshotUrl))
        {
            throw new DomainException("A screenshot of the GCash receipt is required.");
        }

        var reference = referenceNumber?.Trim();

        if (reference is { Length: 0 })
        {
            reference = null;
        }

        if (reference is not null && reference.Length > MaxReferenceLength)
        {
            throw new DomainException($"Reference number cannot exceed {MaxReferenceLength} characters.");
        }

        return new PaymentReceipt(screenshotUrl.Trim(), reference, DateTime.UtcNow);
    }
}
