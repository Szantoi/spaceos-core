// SpaceOS.Infrastructure/Migrations/20260407110000_Migration_0016_PhysicalSpaces.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0016 — Creates the PhysicalSpaces table with RLS, ownership, cell-size immutability trigger,
/// and indexes (BE-P3A-01, SEC-P3A-01, SEC-P3A-09).
/// <para>
/// All DDL uses <c>suppressTransaction: true</c> for PostgreSQL CONCURRENTLY index support.
/// </para>
/// </summary>
public partial class Migration_0016_PhysicalSpaces : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "PhysicalSpaces" (
                "Id"               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                "TenantId"         UUID        NOT NULL,
                "FacilityId"       UUID        NOT NULL,
                "WidthMm"          INTEGER     NOT NULL CHECK ("WidthMm"  > 0),
                "HeightMm"         INTEGER     NOT NULL CHECK ("HeightMm" > 0),
                "DepthMm"          INTEGER     NOT NULL CHECK ("DepthMm"  > 0),
                "OriginX"          INTEGER     NOT NULL DEFAULT 0,
                "OriginY"          INTEGER     NOT NULL DEFAULT 0,
                "OriginZ"          INTEGER     NOT NULL DEFAULT 0,
                "SpaceType"        VARCHAR(20) NOT NULL
                                       CHECK ("SpaceType" IN ('Room','Corridor','Exterior','Shaft')),
                "CellSizeMm"       INTEGER     NOT NULL DEFAULT 500
                                       CHECK ("CellSizeMm" >= 100),
                "RegistrationHash" VARCHAR(64) NOT NULL,
                "IsArchived"       BOOLEAN     NOT NULL DEFAULT false,
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT now(),
                "UpdatedAt"        TIMESTAMPTZ NOT NULL DEFAULT now(),

                CONSTRAINT "FK_PhysicalSpaces_Facilities"
                    FOREIGN KEY ("FacilityId") REFERENCES "Facilities"("Id") ON DELETE NO ACTION
            );
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "PhysicalSpaces" OWNER TO spaceos_schema_owner;
            GRANT SELECT, INSERT, UPDATE, DELETE ON "PhysicalSpaces" TO spaceos_app;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "PhysicalSpaces" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "PhysicalSpaces" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "ps_tenant_isolation" ON "PhysicalSpaces" FOR ALL
                USING ("TenantId" = COALESCE(
                    NULLIF(current_setting('app.current_tenant_id', true), ''),
                    '00000000-0000-0000-0000-000000000000')::uuid);
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION prevent_cell_size_change() RETURNS TRIGGER AS $$
            BEGIN
                IF OLD."CellSizeMm" != NEW."CellSizeMm" THEN
                    IF EXISTS (SELECT 1 FROM "BvhNodes"
                               WHERE "PhysicalSpaceId" = NEW."Id" LIMIT 1) THEN
                        RAISE EXCEPTION 'cell_size_immutable: BvhNodes already exist for space %', NEW."Id";
                    END IF;
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER "TR_PhysicalSpaces_CellSizeImmutable"
                BEFORE UPDATE ON "PhysicalSpaces"
                FOR EACH ROW EXECUTE FUNCTION prevent_cell_size_change();
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_PhysicalSpaces_TenantId"
            ON "PhysicalSpaces" ("TenantId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_PhysicalSpaces_FacilityId"
            ON "PhysicalSpaces" ("FacilityId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_PhysicalSpaces_Active"
            ON "PhysicalSpaces" ("TenantId", "FacilityId") WHERE "IsArchived" = false
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_PhysicalSpaces_Active\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_PhysicalSpaces_FacilityId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_PhysicalSpaces_TenantId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS \"TR_PhysicalSpaces_CellSizeImmutable\" ON \"PhysicalSpaces\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP FUNCTION IF EXISTS prevent_cell_size_change()",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP POLICY IF EXISTS \"ps_tenant_isolation\" ON \"PhysicalSpaces\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TABLE IF EXISTS \"PhysicalSpaces\"",
            suppressTransaction: true);
    }
}
