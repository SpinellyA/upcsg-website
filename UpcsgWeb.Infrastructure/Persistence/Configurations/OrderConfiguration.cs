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

        // Stored as text. An int ordinal would silently remap every existing row if a
        // stage were ever inserted into the middle of the enum.
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Note).HasMaxLength(1000);
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.ReceiptRejectionReason).HasMaxLength(500);

        // Optional owned type: columns are nullable, and the whole receipt reads as
        // null until the guilder submits one. The length mirrors the domain's own limit
        // so the column can never be narrower than what the aggregate accepts.
        builder.OwnsOne(o => o.Receipt, receipt =>
        {
            receipt.Property(r => r.ReferenceNumber)
                .HasColumnName("ReceiptReference")
                .HasMaxLength(PaymentReceipt.MaxReferenceLength);

            receipt.Property(r => r.ScreenshotUrl)
                .HasColumnName("ReceiptScreenshotUrl")
                .HasMaxLength(500);

            receipt.Property(r => r.SubmittedAt)
                .HasColumnName("ReceiptSubmittedAt");
        });

        // No navigation to AppUser: aggregates reference each other by id only, so
        // loading an order can never drag a user graph along with it.
        builder.Property(o => o.UserId).IsRequired();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);

        // Lines live behind a read-only property backed by a private list, so EF is
        // told to write through the field rather than the (non-existent) setter.
        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Total is computed from the lines; persisting it would let it drift.
        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.IsEditable);
        builder.Ignore(o => o.IsOpen);
        builder.Ignore(o => o.AwaitsPayment);
    }
}
