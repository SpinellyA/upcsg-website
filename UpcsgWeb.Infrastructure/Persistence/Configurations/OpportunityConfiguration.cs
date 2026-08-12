using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("Opportunities");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Title).HasMaxLength(250).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(4000);
        builder.Property(o => o.Organiser).HasMaxLength(200);
        builder.Property(o => o.Location).HasMaxLength(200);
        builder.Property(o => o.Url).HasMaxLength(500);
        builder.Property(o => o.PosterUrl).HasMaxLength(500);

        builder.Property(o => o.Kind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(o => o.ClosesAt);
        builder.HasIndex(o => o.Kind);
    }
}
