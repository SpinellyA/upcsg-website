using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderRefundDue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaidAmount",
                table: "Orders",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmountPaidCurrency",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReference",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundSettledAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortfallReason",
                table: "OrderLines",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrderLines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ToFulfil");
            // Status must default to a real enum name. The scaffolder used "", which does
            // not parse back to OrderLineStatus and would break every existing line on read.

            // Orders past the payment gate were paid in full at their own total; recording
            // that keeps the new AmountPaid column meaningful for history rather than null.
            migrationBuilder.Sql("""
                UPDATE "Orders" o
                SET "AmountPaidAmount" = COALESCE((
                        SELECT SUM(l."UnitPriceAmount" * l."Quantity")
                        FROM "OrderLines" l WHERE l."OrderId" = o."Id"
                    ), 0),
                    "AmountPaidCurrency" = 'PHP'
                WHERE o."Status" IN ('Acknowledged', 'Released', 'Received');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AmountPaidCurrency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundSettledAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShortfallReason",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderLines");
        }
    }
}
