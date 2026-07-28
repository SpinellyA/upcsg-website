using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Merch;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class MerchItemConfiguration : IEntityTypeConfiguration<MerchItem>
{
    public void Configure(EntityTypeBuilder<MerchItem> builder)
    {
        builder.ToTable("MerchItems");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.ImageUrl).HasMaxLength(500);

        builder.OwnsOne(m => m.Price, price =>
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

        // Postgres stores string[] natively, so variants need no join table.
        builder.Property<List<string>>("_variants")
            .HasColumnName("Variants")
            .HasColumnType("text[]")
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.Variants);
    }
}
