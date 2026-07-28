using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Settings;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(s => s.Id);
    }
}
