using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UpcsgWeb.Infrastructure.Migrations
{
    /// <summary>
    /// Variants stop being a text[] of names and become rows carrying their own price and
    /// photos; MerchItems gains an ordered photo list and loses its single ImageUrl.
    ///
    /// Hand-edited. The scaffolded version RENAMED "Variants" to "PhotoUrls", which would
    /// have silently reinterpreted every size name ("S", "M", "L") as a photo URL, and
    /// dropped ImageUrl without moving it anywhere. This version copies both across before
    /// removing either column.
    /// </summary>
    public partial class MerchVariantsAndPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MerchVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchItemId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    PhotoUrls = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchVariants_MerchItems_MerchItemId",
                        column: x => x.MerchItemId,
                        principalTable: "MerchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Each existing variant name becomes a row at the item's current price, keeping
            // its position. Cart and order lines match variants by name, so preserving the
            // exact strings is what keeps existing carts resolving.
            //
            // DISTINCT ON guards the unique index below: the old text[] had no constraint,
            // so a hand-edited row could hold the same name twice.
            migrationBuilder.Sql("""
                INSERT INTO "MerchVariants"
                    ("MerchItemId", "Name", "Description", "PriceAmount", "PriceCurrency", "DisplayOrder", "PhotoUrls")
                SELECT DISTINCT ON (m."Id", lower(v.name))
                    m."Id",
                    v.name,
                    '',
                    m."PriceAmount",
                    m."PriceCurrency",
                    (v.ord - 1)::int,
                    ARRAY[]::text[]
                FROM "MerchItems" m
                CROSS JOIN LATERAL unnest(m."Variants") WITH ORDINALITY AS v(name, ord)
                WHERE m."Variants" IS NOT NULL
                  AND btrim(v.name) <> ''
                ORDER BY m."Id", lower(v.name), v.ord;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MerchVariants_MerchItemId_DisplayOrder",
                table: "MerchVariants",
                columns: new[] { "MerchItemId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MerchVariants_MerchItemId_Name",
                table: "MerchVariants",
                columns: new[] { "MerchItemId", "Name" },
                unique: true);

            // A new column, not a rename — see the note above.
            migrationBuilder.AddColumn<List<string>>(
                name: "PhotoUrls",
                table: "MerchItems",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.Sql("""
                UPDATE "MerchItems"
                SET "PhotoUrls" = ARRAY["ImageUrl"]
                WHERE "ImageUrl" IS NOT NULL AND btrim("ImageUrl") <> '';
                """);

            migrationBuilder.DropColumn(name: "Variants", table: "MerchItems");
            migrationBuilder.DropColumn(name: "ImageUrl", table: "MerchItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Variants",
                table: "MerchItems",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MerchItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Fold the rows back into the array, in display order.
            migrationBuilder.Sql("""
                UPDATE "MerchItems" m
                SET "Variants" = COALESCE((
                    SELECT array_agg(v."Name" ORDER BY v."DisplayOrder")
                    FROM "MerchVariants" v
                    WHERE v."MerchItemId" = m."Id"
                ), ARRAY[]::text[]);
                """);

            // Per-variant prices and any photo past the first have nowhere to go in the old
            // shape, so rolling back loses them. Stated rather than hidden.
            migrationBuilder.Sql("""
                UPDATE "MerchItems"
                SET "ImageUrl" = "PhotoUrls"[1]
                WHERE array_length("PhotoUrls", 1) >= 1;
                """);

            migrationBuilder.DropTable(name: "MerchVariants");
            migrationBuilder.DropColumn(name: "PhotoUrls", table: "MerchItems");
        }
    }
}
