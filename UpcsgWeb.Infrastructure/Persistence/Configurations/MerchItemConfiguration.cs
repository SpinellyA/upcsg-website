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

        // Postgres stores string[] natively, so an ordered photo list needs no join table.
        // Order is meaningful — the first entry is what listings show.
        builder.Property<List<string>>("_photoUrls")
            .HasColumnName("PhotoUrls")
            .HasColumnType("text[]")
            .HasField("_photoUrls")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.PhotoUrls);

        // Variants were a text[] of names. They are entities now: each has its own price
        // and photos, which an array column cannot carry.
        builder.HasMany<MerchVariant>("_variants")
            .WithOne()
            .HasForeignKey(v => v.MerchItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_variants")
            .HasField("_variants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.Variants);
        builder.Ignore(m => m.PriceFrom);
        builder.Ignore(m => m.HasPriceRange);
    }
}
