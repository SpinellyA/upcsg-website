using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Domain.Orders;

public sealed record PaymentReceipt
{
    public const int MaxReferenceLength = 50;

    public string ScreenshotUrl { get; }

    public string? ReferenceNumber { get; }

    public DateTime SubmittedAt { get; }

    private PaymentReceipt(string screenshotUrl, string? referenceNumber, DateTime submittedAt)
    {
        ScreenshotUrl = screenshotUrl;
        ReferenceNumber = referenceNumber;
        SubmittedAt = submittedAt;
    }

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
