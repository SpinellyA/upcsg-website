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

        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);

        builder.Property(m => m.SalePercentage).HasPrecision(5, 2);

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

        builder.Property<List<string>>("_photoUrls")
            .HasColumnName("PhotoUrls")
            .HasColumnType("text[]")
            .HasField("_photoUrls")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.PhotoUrls);

        builder.HasMany<MerchVariant>("_variants")
            .WithOne()
            .HasForeignKey(v => v.MerchItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_variants")
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.Variants);

        builder.Ignore(m => m.PriceFrom);
        builder.Ignore(m => m.ListPriceFrom);
        builder.Ignore(m => m.HasPriceRange);
        builder.Ignore(m => m.HasActiveSale);
        builder.Ignore(m => m.IsPreorderClosed);
    }
}
