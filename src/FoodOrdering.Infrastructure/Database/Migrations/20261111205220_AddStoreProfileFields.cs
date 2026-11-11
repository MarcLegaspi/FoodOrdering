using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "stores",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "stores",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Address", "OpeningHours" },
                values: new object[] { "101 Grill Ave, Downtown", "Daily 10:00 AM - 10:00 PM" });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "Address", "OpeningHours" },
                values: new object[] { "202 Brick Oven St, Midtown", "Daily 11:00 AM - 11:00 PM" });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "Address", "OpeningHours" },
                values: new object[] { "303 Harbor Way, Waterfront", "Tue-Sun 11:30 AM - 9:30 PM" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "stores");
        }
    }
}
