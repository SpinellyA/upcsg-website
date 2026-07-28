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

    public string ReferenceNumber { get; }

    /// <summary>Uploaded screenshot. Optional until file storage exists.</summary>
    public string? ScreenshotUrl { get; }

    public DateTime SubmittedAt { get; }

    private PaymentReceipt(string referenceNumber, string? screenshotUrl, DateTime submittedAt)
    {
        ReferenceNumber = referenceNumber;
        ScreenshotUrl = screenshotUrl;
        SubmittedAt = submittedAt;
    }

    public static PaymentReceipt Submit(string referenceNumber, string? screenshotUrl)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new DomainException("A GCash reference number is required.");
        }

        var trimmed = referenceNumber.Trim();

        if (trimmed.Length > MaxReferenceLength)
        {
            throw new DomainException($"Reference number cannot exceed {MaxReferenceLength} characters.");
        }

        return new PaymentReceipt(trimmed, screenshotUrl, DateTime.UtcNow);
    }
}
