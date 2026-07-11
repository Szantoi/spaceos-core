// SpaceOS.Infrastructure/Migrations/20260407140000_Migration_0019_SpatialTaskLinks.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0019 — Creates the try_cast_uuid helper function and SpatialTaskLinks table
/// with RLS, ownership, cross-tenant trigger, and indexes (SEC-P3A-01, SEC-P3A-02, SEC-P3A-08).
/// </summary>
public partial class Migration_0019_SpatialTaskLinks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION try_cast_uuid(text)
            RETURNS UUID AS $$
            BEGIN RETURN $1::uuid;
            EXCEPTION WHEN invalid_text_representation THEN RETURN NULL;
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE TABLE "SpatialTaskLinks" (
                "Id"               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                "TenantId"         UUID        NOT NULL,
                "FlowTaskId"       UUID        NOT NULL,
                "SpatialElementId" UUID        NOT NULL,
                "WorkPhase"        VARCHAR(50) NOT NULL
                                       CHECK ("WorkPhase" IN
                                           ('measurement','cutting','edging',
                                            'assembly','finishing','installation')),
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT now(),

                CONSTRAINT "FK_SpatialTaskLinks_FlowTask"
                    FOREIGN KEY ("FlowTaskId") REFERENCES "FlowTasks"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SpatialTaskLinks_SpatialElement"
                    FOREIGN KEY ("SpatialElementId") REFERENCES "SpatialElements"("Id") ON DELETE CASCADE,
                CONSTRAINT "UQ_SpatialTaskLinks_TaskElement"
                    UNIQUE ("FlowTaskId", "SpatialElementId")
            );
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "SpatialTaskLinks" OWNER TO spaceos_schema_owner;
            GRANT SELECT, INSERT, UPDATE, DELETE ON "SpatialTaskLinks" TO spaceos_app;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "SpatialTaskLinks" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "SpatialTaskLinks" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "stl_tenant_isolation" ON "SpatialTaskLinks" FOR ALL
                USING ("TenantId" = COALESCE(
                    NULLIF(current_setting('app.current_tenant_id', true), ''),
                    '00000000-0000-0000-0000-000000000000')::uuid);
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION check_spatial_task_link_tenant() RETURNS TRIGGER AS $$
            DECLARE
                v_task_tenant    UUID;
                v_element_tenant UUID;
            BEGIN
                SELECT "TenantId" INTO v_task_tenant
                    FROM "FlowTasks" WHERE "Id" = NEW."FlowTaskId";
                SELECT "TenantId" INTO v_element_tenant
                    FROM "SpatialElements" WHERE "Id" = NEW."SpatialElementId";
                IF v_task_tenant IS DISTINCT FROM v_element_tenant THEN
                    RAISE EXCEPTION 'cross_tenant_link_rejected: task % != element %',
                        v_task_tenant, v_element_tenant;
                END IF;
                IF NEW."TenantId" IS DISTINCT FROM v_task_tenant THEN
                    RAISE EXCEPTION 'spatial_task_link_tenant_mismatch';
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER "TR_SpatialTaskLinks_TenantCheck"
                BEFORE INSERT ON "SpatialTaskLinks"
                FOR EACH ROW EXECUTE FUNCTION check_spatial_task_link_tenant();
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpatialTaskLinks_FlowTaskId"
            ON "SpatialTaskLinks" ("FlowTaskId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpatialTaskLinks_SpatialElementId"
            ON "SpatialTaskLinks" ("SpatialElementId")
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpatialTaskLinks_SpatialElementId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpatialTaskLinks_FlowTaskId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS \"TR_SpatialTaskLinks_TenantCheck\" ON \"SpatialTaskLinks\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP FUNCTION IF EXISTS check_spatial_task_link_tenant()",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP POLICY IF EXISTS \"stl_tenant_isolation\" ON \"SpatialTaskLinks\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TABLE IF EXISTS \"SpatialTaskLinks\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP FUNCTION IF EXISTS try_cast_uuid(text)",
            suppressTransaction: true);
    }
}
