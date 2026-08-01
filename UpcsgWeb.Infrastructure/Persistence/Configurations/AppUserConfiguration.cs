using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // Create assigns the id, so the store must never substitute one.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.GoogleSubject).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PictureUrl).HasMaxLength(1000);
        builder.Property(u => u.Role).HasMaxLength(50).IsRequired();

        // The database refuses duplicates even if two sign-ins race each other.
        builder.HasIndex(u => u.GoogleSubject).IsUnique();
        builder.HasIndex(u => u.Email);

        builder.Ignore(u => u.IsAdmin);
    }
}
