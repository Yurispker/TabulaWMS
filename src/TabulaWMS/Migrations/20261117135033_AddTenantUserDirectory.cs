using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabulaWMS.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantUserDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "TenantUserDirectory",
                schema: "public",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserDirectory", x => x.Email);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserDirectory_Email",
                schema: "public",
                table: "TenantUserDirectory",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantUserDirectory",
                schema: "public");
        }
    }
}
