using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeSnapshot",
                table: "orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promo_codes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_codes", x => x.Id);
                    table.CheckConstraint("CK_promo_codes_DiscountValue_Positive", "\"DiscountValue\" > 0");
                    table.CheckConstraint("CK_promo_codes_UsedCount_NonNegative", "\"UsedCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_promo_codes_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_DiscountAmount_NonNegative",
                table: "orders",
                sql: "\"DiscountAmount\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_PublicId",
                table: "promo_codes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_StoreId_Code",
                table: "promo_codes",
                columns: new[] { "StoreId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promo_codes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_DiscountAmount_NonNegative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeSnapshot",
                table: "orders");
        }
    }
}
