using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReviewsAndStoreRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "stores",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "stores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "order_reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_reviews", x => x.Id);
                    table.CheckConstraint("CK_order_reviews_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
                    table.ForeignKey(
                        name: "FK_order_reviews_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_reviews_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "AverageRating", "ReviewCount" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "AverageRating", "ReviewCount" },
                values: new object[] { null, 0 });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "AverageRating", "ReviewCount" },
                values: new object[] { null, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_order_reviews_OrderId",
                table: "order_reviews",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_reviews_PublicId",
                table: "order_reviews",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_reviews_StoreId_CreatedAtUtc",
                table: "order_reviews",
                columns: new[] { "StoreId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_reviews");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "stores");
        }
    }
}
