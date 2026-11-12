using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "menu_item_option_groups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_option_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_menu_item_option_groups_menu_items_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "menu_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item_options",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    GroupNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OptionNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceDeltaSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_options", x => x.Id);
                    table.CheckConstraint("CK_order_item_options_PriceDeltaSnapshot_NonNegative", "\"PriceDeltaSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_order_item_options_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_options",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemOptionGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceDelta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_item_options", x => x.Id);
                    table.CheckConstraint("CK_menu_item_options_PriceDelta_NonNegative", "\"PriceDelta\" >= 0");
                    table.ForeignKey(
                        name: "FK_menu_item_options_menu_item_option_groups_MenuItemOptionGro~",
                        column: x => x.MenuItemOptionGroupId,
                        principalTable: "menu_item_option_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_option_groups_MenuItemId",
                table: "menu_item_option_groups",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_option_groups_PublicId",
                table: "menu_item_option_groups",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_options_MenuItemOptionGroupId",
                table: "menu_item_options",
                column: "MenuItemOptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_item_options_PublicId",
                table: "menu_item_options",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_item_options_OrderItemId",
                table: "order_item_options",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "menu_item_options");

            migrationBuilder.DropTable(
                name: "order_item_options");

            migrationBuilder.DropTable(
                name: "menu_item_option_groups");
        }
    }
}
