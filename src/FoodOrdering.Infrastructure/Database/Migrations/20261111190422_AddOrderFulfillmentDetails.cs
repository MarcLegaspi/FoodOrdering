using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFulfillmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderType",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "orders");
        }
    }
}
