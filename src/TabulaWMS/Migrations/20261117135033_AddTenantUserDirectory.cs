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
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS public."TenantUserDirectory" (
                    "Email" character varying(256) NOT NULL,
                    "TenantId" character varying(64) NOT NULL,
                    CONSTRAINT "PK_TenantUserDirectory" PRIMARY KEY ("Email")
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty or guarded so dropping a tenant doesn't drop the shared public directory
        }

    }
}
