using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UpcsgWeb.Domain.Users;

namespace UpcsgWeb.Infrastructure.Persistence.Configurations;

public class OfficerEmailConfiguration : IEntityTypeConfiguration<OfficerEmail>
{
    private static readonly Guid FoundingOfficerId =
        new("0f0f0f0f-0000-4000-8000-000000000001");

    public void Configure(EntityTypeBuilder<OfficerEmail> builder)
    {
        builder.ToTable("OfficerEmails");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Email).HasMaxLength(320).IsRequired();
        builder.Property(o => o.Note).HasMaxLength(200);

        builder.HasIndex(o => o.Email).IsUnique();

        builder.HasData(new
        {
            Id = FoundingOfficerId,
            Email = "accabildo@up.edu.ph",
            Note = (string?)"Founding officer — seeded so a fresh deployment has a way in.",
            AddedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
