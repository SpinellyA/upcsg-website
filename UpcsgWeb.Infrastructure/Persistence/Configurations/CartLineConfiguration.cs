using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class CartLineConfiguration : IEntityTypeConfiguration<CartLine>
{
    public void Configure(EntityTypeBuilder<CartLine> builder)
    {
        builder.ToTable("CartLines");
        builder.HasKey(l => l.Id);

        // Create assigns the id, so the store must never substitute one.
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Variant).HasMaxLength(100);
        builder.Property(l => l.Quantity).IsRequired();

        // Reference to MerchItem by id, no FK: a cart line must not block deleting a
        // discontinued item, and checkout re-validates the item anyway.
        builder.Property(l => l.MerchItemId).IsRequired();

        // No price column here on purpose — carts price live. See CartLine.
        builder.HasIndex(l => new { l.CartId, l.MerchItemId, l.Variant });
    }
}
