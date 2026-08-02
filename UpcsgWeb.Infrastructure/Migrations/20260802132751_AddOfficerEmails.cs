using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OfficerEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerEmails", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OfficerEmails",
                columns: new[] { "Id", "AddedAt", "Email", "Note" },
                values: new object[] { new Guid("0f0f0f0f-0000-4000-8000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "accabildo@up.edu.ph", "Founding officer — seeded so a fresh deployment has a way in." });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerEmails_Email",
                table: "OfficerEmails",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficerEmails");
        }
    }
}
