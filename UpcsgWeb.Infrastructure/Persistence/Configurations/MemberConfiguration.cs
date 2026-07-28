using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Role).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Committee).HasMaxLength(150);
        builder.Property(m => m.PhotoUrl).HasMaxLength(500);
        builder.Property(m => m.Quote).HasMaxLength(500);
        builder.Property(m => m.Bio).HasMaxLength(4000);

        builder.Property(m => m.Category).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Matches MemberRepository's default ordering.
        builder.HasIndex(m => new { m.Category, m.DisplayOrder });
    }
}
