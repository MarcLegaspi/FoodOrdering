using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedMenuData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "menu_items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:IdentitySequenceOptions", "'1100', '1', '', '', 'False', '1'")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "categories",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:IdentitySequenceOptions", "'1100', '1', '', '', 'False', '1'")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Name", "PublicId", "SortOrder", "StoreId", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1001L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Burgers", new Guid("a1111111-1111-1111-1111-111111111111"), 1, 1L, null },
                    { 1002L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Sides & Drinks", new Guid("a2222222-2222-2222-2222-222222222222"), 2, 1L, null },
                    { 1003L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Classic Pizzas", new Guid("b1111111-1111-1111-1111-111111111111"), 1, 2L, null },
                    { 1004L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Sides & Drinks", new Guid("b2222222-2222-2222-2222-222222222222"), 2, 2L, null },
                    { 1005L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Signature Rolls", new Guid("c1111111-1111-1111-1111-111111111111"), 1, 3L, null },
                    { 1006L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Soups & Sides", new Guid("c2222222-2222-2222-2222-222222222222"), 2, 3L, null }
                });

            migrationBuilder.InsertData(
                table: "menu_items",
                columns: new[] { "Id", "CategoryId", "CreatedAtUtc", "Description", "ImageUrl", "IsAvailable", "Name", "Price", "PublicId", "SortOrder", "StoreId", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1001L, 1001L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Grilled beef patty, cheddar, lettuce, tomato, house sauce", "/images/menu/cheeseburger.jpg", true, "Classic Cheeseburger", 9.99m, new Guid("d1111111-1111-1111-1111-111111111111"), 1, 1L, null },
                    { 1002L, 1002L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Golden sea-salted shoestring fries", "/images/menu/fries.jpg", true, "Crispy Fries", 3.99m, new Guid("d2222222-2222-2222-2222-222222222222"), 1, 1L, null },
                    { 1003L, 1003L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "San Marzano tomato, fresh mozzarella, sweet basil", "/images/menu/margherita-pizza.jpg", true, "Margherita Pizza", 13.50m, new Guid("e1111111-1111-1111-1111-111111111111"), 1, 2L, null },
                    { 1004L, 1004L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Warm oven-baked knots brushed with herb garlic butter", "/images/menu/garlic-knots.jpg", true, "Garlic Knots", 5.50m, new Guid("e2222222-2222-2222-2222-222222222222"), 1, 2L, null },
                    { 1005L, 1005L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fresh Atlantic salmon, ripe avocado, toasted sesame", "/images/menu/salmon-roll.jpg", true, "Salmon Avocado Roll", 11.50m, new Guid("f1111111-1111-1111-1111-111111111111"), 1, 3L, null },
                    { 1006L, 1006L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Traditional dashi broth with tofu, wakame, and scallions", "/images/menu/miso-soup.jpg", true, "Miso Soup", 3.50m, new Guid("f2222222-2222-2222-2222-222222222222"), 1, 3L, null }
                });

            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('categories', 'Id'), GREATEST((SELECT COALESCE(MAX(\"Id\"), 1) FROM categories), 1100));");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('menu_items', 'Id'), GREATEST((SELECT COALESCE(MAX(\"Id\"), 1) FROM menu_items), 1100));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1001L);

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1002L);

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1003L);

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1004L);

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1005L);

            migrationBuilder.DeleteData(
                table: "menu_items",
                keyColumn: "Id",
                keyValue: 1006L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1001L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1002L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1003L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1004L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1005L);

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1006L);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "menu_items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:IdentitySequenceOptions", "'1100', '1', '', '', 'False', '1'")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "categories",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:IdentitySequenceOptions", "'1100', '1', '', '', 'False', '1'")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
