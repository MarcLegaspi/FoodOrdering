using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAcceptingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptingOrders",
                table: "stores",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 1L,
                column: "IsAcceptingOrders",
                value: true);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 2L,
                column: "IsAcceptingOrders",
                value: true);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 3L,
                column: "IsAcceptingOrders",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAcceptingOrders",
                table: "stores");
        }
    }
}
