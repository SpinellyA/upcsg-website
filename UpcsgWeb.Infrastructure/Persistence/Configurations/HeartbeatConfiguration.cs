using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class HeartbeatConfiguration : IEntityTypeConfiguration<Heartbeat>
{
    public void Configure(EntityTypeBuilder<Heartbeat> builder)
    {
        builder.ToTable("Heartbeats");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.HasData(new Heartbeat
        {
            Id = Heartbeat.SingletonId,
            LastPingedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PingCount = 0,
        });
    }
}
