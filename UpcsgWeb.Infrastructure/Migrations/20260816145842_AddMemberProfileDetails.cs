using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberProfileDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Achievements",
                table: "Members",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Links",
                table: "Members",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profile",
                table: "Members",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Achievements",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Links",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "Members");
        }
    }
}
