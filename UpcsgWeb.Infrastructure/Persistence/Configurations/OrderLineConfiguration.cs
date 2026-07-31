using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Orders;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ItemName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Variant).HasMaxLength(100);
        builder.Property(l => l.Quantity).IsRequired();

        // Reference to the MerchItem aggregate by id, with no FK constraint: deleting
        // a discontinued item must not cascade into historical orders, and the line
        // already carries its own name/price snapshot.
        builder.Property(l => l.MerchItemId).IsRequired();
        builder.HasIndex(l => l.MerchItemId);

        builder.OwnsOne(l => l.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("UnitPriceAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Text, for the same reason as OrderStatus: an ordinal would silently remap every
        // existing row if a value were ever inserted mid-enum.
        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ShortfallReason).HasMaxLength(500);

        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.IsRefundDue);
    }
}
