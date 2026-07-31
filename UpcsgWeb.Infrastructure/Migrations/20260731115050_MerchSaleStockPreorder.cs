using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MerchSaleStockPreorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "MerchVariants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnSale",
                table: "MerchItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPreorder",
                table: "MerchItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreorderClosesAt",
                table: "MerchItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePercentage",
                table: "MerchItems",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "MerchItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "MerchVariants");

            migrationBuilder.DropColumn(
                name: "IsOnSale",
                table: "MerchItems");

            migrationBuilder.DropColumn(
                name: "IsPreorder",
                table: "MerchItems");

            migrationBuilder.DropColumn(
                name: "PreorderClosesAt",
                table: "MerchItems");

            migrationBuilder.DropColumn(
                name: "SalePercentage",
                table: "MerchItems");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "MerchItems");
        }
    }
}
