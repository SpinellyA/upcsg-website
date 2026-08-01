using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Carts;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);

        // Create assigns the id, so the store must never substitute one.
        builder.Property(c => c.Id).ValueGeneratedNever();

        // One cart per guilder, enforced by the database rather than by hoping the
        // application never races itself.
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.HasMany(c => c.Lines)
            .WithOne()
            .HasForeignKey(l => l.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Cart.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.IsEmpty);
        builder.Ignore(c => c.TotalItems);
    }
}
