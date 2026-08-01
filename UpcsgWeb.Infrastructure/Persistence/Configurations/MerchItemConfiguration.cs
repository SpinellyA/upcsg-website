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

        // Create assigns the id, so the store must never substitute one.
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(2000);

        // The percentage, not the discounted amount — see the note on MerchItem.
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

        // Derived from the columns above; nothing to persist.
        builder.Ignore(m => m.PriceFrom);
        builder.Ignore(m => m.ListPriceFrom);
        builder.Ignore(m => m.HasPriceRange);
        builder.Ignore(m => m.HasActiveSale);
        builder.Ignore(m => m.IsPreorderClosed);
    }
}
