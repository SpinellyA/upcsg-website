using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Stored as text like Status, so the column reads plainly in the database rather than
        // as an integer whose meaning shifts if the enum is ever reordered. Existing rows all
        // predate cash, so GCash is the correct default for them.
        builder.Property(o => o.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentMethod.GCash)
            .IsRequired();

        builder.Property(o => o.Note).HasMaxLength(1000);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.ReceiptRejectionReason).HasMaxLength(500);

        builder.OwnsOne(o => o.AmountPaid, paid =>
        {
            paid.Property(p => p.Amount)
                .HasColumnName("AmountPaidAmount")
                .HasPrecision(10, 2);

            paid.Property(p => p.Currency)
                .HasColumnName("AmountPaidCurrency")
                .HasMaxLength(3);
        });

        builder.Property(o => o.RefundReference).HasMaxLength(PaymentReceipt.MaxReferenceLength);

        builder.OwnsOne(o => o.Receipt, receipt =>
        {
            receipt.Property(r => r.ReferenceNumber)
                .HasColumnName("ReceiptReference")
                .HasMaxLength(PaymentReceipt.MaxReferenceLength);

            receipt.Property(r => r.ScreenshotUrl)
                .HasColumnName("ReceiptScreenshotUrl")
                .IsRequired(false)
                .HasMaxLength(500);

            receipt.Property(r => r.SubmittedAt)
                .HasColumnName("ReceiptSubmittedAt");
        });

        builder.Property(o => o.UserId).IsRequired();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.IsEditable);
        builder.Ignore(o => o.IsOpen);
        builder.Ignore(o => o.AwaitsPayment);

        builder.Ignore(o => o.RefundDue);
        builder.Ignore(o => o.HasRefundDue);
        builder.Ignore(o => o.RefundSettled);
        builder.Ignore(o => o.FulfilledTotal);
    }
}
