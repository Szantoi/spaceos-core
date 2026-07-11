// SpaceOS.Infrastructure/Migrations/20260408100000_Migration_0025_TenantEnabledModules.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0025 — Adds the <c>EnabledModules</c> array column to the <c>Tenants</c> table.
/// <para>
/// The column stores a PostgreSQL <c>varchar(32)[]</c> array with a CHECK constraint
/// ensuring only valid module names ("door", "cabinet", "window") are persisted.
/// Existing tenants with <c>BrandSkinId = 'doorstar'</c> are seeded with <c>ARRAY['door']</c>.
/// </para>
/// </summary>
public partial class Migration_0025_TenantEnabledModules : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Tenants" ADD COLUMN IF NOT EXISTS "EnabledModules" varchar(32)[] NOT NULL DEFAULT '{}';
            ALTER TABLE "Tenants" DROP CONSTRAINT IF EXISTS "CK_Tenants_EnabledModules_Valid";
            ALTER TABLE "Tenants" ADD CONSTRAINT "CK_Tenants_EnabledModules_Valid"
              CHECK ("EnabledModules" <@ ARRAY['door','cabinet','window']::varchar(32)[]);
            UPDATE "Tenants" SET "EnabledModules" = ARRAY['door'] WHERE "BrandSkinId" = 'doorstar';
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Tenants" DROP CONSTRAINT IF EXISTS "CK_Tenants_EnabledModules_Valid";
            ALTER TABLE "Tenants" DROP COLUMN IF EXISTS "EnabledModules";
            """,
            suppressTransaction: true);
    }
}
