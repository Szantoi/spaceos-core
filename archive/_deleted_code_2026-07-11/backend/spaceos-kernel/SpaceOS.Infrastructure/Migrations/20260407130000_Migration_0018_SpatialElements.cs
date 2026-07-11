// SpaceOS.Infrastructure/Migrations/20260407130000_Migration_0018_SpatialElements.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0018 — Creates the SpatialElements table, backward FK on BvhNodes,
/// SpatialContractsView, RLS, ownership, and indexes (SEC-P3A-01, SEC-P3A-04, SEC-P3A-10).
/// </summary>
public partial class Migration_0018_SpatialElements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "SpatialElements" (
                "Id"          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                "TenantId"    UUID        NOT NULL,
                "BvhLeafId"   UUID        NOT NULL,
                "FlowEpicId"  UUID        NOT NULL,
                "TradeType"   VARCHAR(50) NOT NULL
                                  CHECK ("TradeType" IN ('door','window','cabinet','wall','opening','shelf')),
                "ElementType" VARCHAR(50) NOT NULL,
                "IsArchived"  BOOLEAN     NOT NULL DEFAULT false,
                "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now(),
                "UpdatedAt"   TIMESTAMPTZ NOT NULL DEFAULT now(),

                CONSTRAINT "FK_SpatialElements_BvhLeaf"
                    FOREIGN KEY ("BvhLeafId") REFERENCES "BvhNodes"("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_SpatialElements_FlowEpic"
                    FOREIGN KEY ("FlowEpicId") REFERENCES "FlowEpics"("Id") ON DELETE NO ACTION,
                CONSTRAINT "UQ_SpatialElements_BvhLeafId" UNIQUE ("BvhLeafId")
            );
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "SpatialElements" OWNER TO spaceos_schema_owner;
            GRANT SELECT, INSERT, UPDATE, DELETE ON "SpatialElements" TO spaceos_app;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "SpatialElements" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "SpatialElements" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "se_tenant_isolation" ON "SpatialElements" FOR ALL
                USING ("TenantId" = COALESCE(
                    NULLIF(current_setting('app.current_tenant_id', true), ''),
                    '00000000-0000-0000-0000-000000000000')::uuid);
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "BvhNodes"
                ADD CONSTRAINT "FK_BvhNodes_SpatialElement"
                FOREIGN KEY ("ElementId") REFERENCES "SpatialElements"("Id")
                ON DELETE SET NULL;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE VIEW "SpatialContractsView" AS
            SELECT
                se."Id"        AS "ElementId",
                se."TenantId",
                se."TradeType",
                se."FlowEpicId",
                bn."MinX", bn."MinY", bn."MinZ",
                bn."MaxX", bn."MaxY", bn."MaxZ",
                se."IsArchived",
                se."CreatedAt"
            FROM "SpatialElements" se
            JOIN "BvhNodes" bn ON bn."Id" = se."BvhLeafId"
            WHERE se."IsArchived" = false;
            GRANT SELECT ON "SpatialContractsView" TO spaceos_app;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpatialElements_TenantId"
            ON "SpatialElements" ("TenantId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpatialElements_FlowEpicId"
            ON "SpatialElements" ("FlowEpicId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpatialElements_TradeType"
            ON "SpatialElements" ("TradeType") WHERE "IsArchived" = false
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpatialElements_TradeType\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpatialElements_FlowEpicId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpatialElements_TenantId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP VIEW IF EXISTS \"SpatialContractsView\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "ALTER TABLE \"BvhNodes\" DROP CONSTRAINT IF EXISTS \"FK_BvhNodes_SpatialElement\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP POLICY IF EXISTS \"se_tenant_isolation\" ON \"SpatialElements\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TABLE IF EXISTS \"SpatialElements\"",
            suppressTransaction: true);
    }
}
