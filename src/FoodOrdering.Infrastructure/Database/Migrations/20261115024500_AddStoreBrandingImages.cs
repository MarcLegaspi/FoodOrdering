using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrdering.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreBrandingImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "stores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "stores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "LogoUrl", "BannerImageUrl" },
                values: new object[] { "/images/stores/burger-barn-logo.svg", "/images/stores/burger-barn.jpg" });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "LogoUrl", "BannerImageUrl" },
                values: new object[] { "/images/stores/pizza-palace-logo.svg", "/images/stores/pizza-palace.jpg" });

            migrationBuilder.UpdateData(
                table: "stores",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "LogoUrl", "BannerImageUrl" },
                values: new object[] { "/images/stores/sushi-spot-logo.svg", "/images/stores/sushi-spot.jpg" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "stores");
        }
    }
}
