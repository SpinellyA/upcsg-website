using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class MerchVariantConfiguration : IEntityTypeConfiguration<MerchVariant>
{
    public void Configure(EntityTypeBuilder<MerchVariant> builder)
    {
        builder.ToTable("MerchVariants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).HasMaxLength(120).IsRequired();
        builder.Property(v => v.Description).HasMaxLength(2000);

        builder.OwnsOne(v => v.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(10, 2)
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property<List<string>>("_photoUrls")
            .HasColumnName("PhotoUrls")
            .HasColumnType("text[]")
            .HasField("_photoUrls")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(v => v.PhotoUrls);

        // Cart and order lines match variants by name, so two variants of one item sharing
        // a name would make those lines ambiguous. The aggregate rejects it; this makes the
        // database refuse it too.
        builder.HasIndex(v => new { v.MerchItemId, v.Name }).IsUnique();

        builder.HasIndex(v => new { v.MerchItemId, v.DisplayOrder });
    }
}
