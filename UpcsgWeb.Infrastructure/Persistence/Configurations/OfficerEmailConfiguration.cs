using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class OfficerEmailConfiguration : IEntityTypeConfiguration<OfficerEmail>
{
    /// <summary>
    /// Fixed so the seed is idempotent: a re-run of the migration updates this row rather
    /// than inserting a second copy of the same address.
    /// </summary>
    private static readonly Guid FoundingOfficerId =
        new("0f0f0f0f-0000-4000-8000-000000000001");

    public void Configure(EntityTypeBuilder<OfficerEmail> builder)
    {
        builder.ToTable("OfficerEmails");
        builder.HasKey(o => o.Id);

        // Create assigns the id, so the store must never substitute one.
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Email).HasMaxLength(320).IsRequired();
        builder.Property(o => o.Note).HasMaxLength(200);

        // Unique even if two officers add the same address at once. The application
        // checks first so the usual path gives a readable message; this is the backstop.
        builder.HasIndex(o => o.Email).IsUnique();

        // The bootstrap officer. Without a seeded row a fresh deployment has an empty
        // allowlist, nobody can reach the admin pages, and the only endpoint that could
        // mint an admin token is excluded from production builds — so the site would
        // ship with no way in at all.
        builder.HasData(new
        {
            Id = FoundingOfficerId,
            Email = "accabildo@up.edu.ph",
            Note = (string?)"Founding officer — seeded so a fresh deployment has a way in.",
            AddedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
