using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabulaWMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuToInventoryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "InventoryItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SKU",
                table: "InventoryItems");
        }
    }
}
