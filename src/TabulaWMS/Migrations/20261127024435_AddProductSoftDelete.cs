using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabulaWMS.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"Products"') IS NOT NULL THEN
                        ALTER TABLE "Products"
                        ADD COLUMN "IsDeleted" boolean NOT NULL DEFAULT FALSE;
                    END IF;
                END;
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"Products"') IS NOT NULL THEN
                        ALTER TABLE "Products"
                        DROP COLUMN IF EXISTS "IsDeleted";
                    END IF;
                END;
                $$;
                """);
        }
    }
}
