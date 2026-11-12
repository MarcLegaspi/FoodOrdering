using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStartsAtUtcToPromoCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAtUtc",
                table: "promo_codes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                table: "promo_codes");
        }
    }
}
