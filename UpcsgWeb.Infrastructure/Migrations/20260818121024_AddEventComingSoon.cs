using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventComingSoon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDateTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "IsDateTentative",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDateTentative",
                table: "Events");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDateTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
