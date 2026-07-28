using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Content;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class GuildEventConfiguration : IEntityTypeConfiguration<GuildEvent>
{
    public void Configure(EntityTypeBuilder<GuildEvent> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.PosterUrl).HasMaxLength(500);

        // Every event read is a month range, so this is the index that matters.
        builder.HasIndex(e => e.StartDateTime);
    }
}
