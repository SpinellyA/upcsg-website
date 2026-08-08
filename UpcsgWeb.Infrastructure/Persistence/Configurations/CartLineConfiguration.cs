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

        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Variant).HasMaxLength(100);
        builder.Property(l => l.Quantity).IsRequired();

        builder.Property(l => l.MerchItemId).IsRequired();

        builder.HasIndex(l => new { l.CartId, l.MerchItemId, l.Variant });
    }
}
