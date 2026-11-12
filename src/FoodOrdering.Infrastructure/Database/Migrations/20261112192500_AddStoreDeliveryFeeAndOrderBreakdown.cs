using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreDeliveryFeeAndOrderBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "stores",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedDeliveryTime",
                table: "stores",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "FreeDeliveryThreshold",
                table: "stores",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumDeliveryOrder",
                table: "stores",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryLandmark",
                table: "orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedDeliveryTime",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE \"orders\" SET \"Subtotal\" = \"TotalAmount\" WHERE \"Subtotal\" = 0 AND \"TotalAmount\" > 0;");

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "EstimatedDeliveryTime", "FreeDeliveryThreshold" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "EstimatedDeliveryTime", "FreeDeliveryThreshold" },
                values: new object[] { "", null });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "EstimatedDeliveryTime", "FreeDeliveryThreshold" },
                values: new object[] { "", null });

            migrationBuilder.AddCheckConstraint(
                name: "CK_stores_DeliveryFee_NonNegative",
                table: "stores",
                sql: "\"DeliveryFee\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_stores_MinimumDeliveryOrder_NonNegative",
                table: "stores",
                sql: "\"MinimumDeliveryOrder\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_DeliveryFee_NonNegative",
                table: "orders",
                sql: "\"DeliveryFee\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_orders_Subtotal_NonNegative",
                table: "orders",
                sql: "\"Subtotal\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_stores_DeliveryFee_NonNegative",
                table: "stores");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stores_MinimumDeliveryOrder_NonNegative",
                table: "stores");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_DeliveryFee_NonNegative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_orders_Subtotal_NonNegative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryTime",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "FreeDeliveryThreshold",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "MinimumDeliveryOrder",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DeliveryLandmark",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryTime",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "orders");
        }
    }
}
