// SpaceOS.Infrastructure/Migrations/20260407120000_Migration_0017_BvhNodes.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0017 — Creates the BvhNodes table with RLS, ownership, depth-limit trigger,
/// and indexes (BE-P3A-03, SEC-P3A-01, SEC-P3A-03, SEC-P3A-07).
/// </summary>
public partial class Migration_0017_BvhNodes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "BvhNodes" (
                "Id"               UUID    NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
                "TenantId"         UUID    NOT NULL,
                "PhysicalSpaceId"  UUID    NOT NULL,
                "ParentId"         UUID    NULL,
                "MinX" INTEGER NOT NULL, "MinY" INTEGER NOT NULL, "MinZ" INTEGER NOT NULL,
                "MaxX" INTEGER NOT NULL, "MaxY" INTEGER NOT NULL, "MaxZ" INTEGER NOT NULL,
                "IsLeaf"           BOOLEAN NOT NULL DEFAULT false,
                "ElementId"        UUID    NULL,
                "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT now(),

                CONSTRAINT "FK_BvhNodes_PhysicalSpace"
                    FOREIGN KEY ("PhysicalSpaceId") REFERENCES "PhysicalSpaces"("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_BvhNodes_Parent"
                    FOREIGN KEY ("ParentId") REFERENCES "BvhNodes"("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_BvhNodes_NoSelfLoop"
                    CHECK ("ParentId" IS NULL OR "ParentId" != "Id"),
                CONSTRAINT "CK_BvhNodes_LeafElement"
                    CHECK (("IsLeaf" = true) OR ("ElementId" IS NULL))
            );
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "BvhNodes" OWNER TO spaceos_schema_owner;
            GRANT SELECT, INSERT, UPDATE, DELETE ON "BvhNodes" TO spaceos_app;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE "BvhNodes" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "BvhNodes" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "bvh_tenant_isolation" ON "BvhNodes" FOR ALL
                USING ("TenantId" = COALESCE(
                    NULLIF(current_setting('app.current_tenant_id', true), ''),
                    '00000000-0000-0000-0000-000000000000')::uuid);
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION check_bvh_depth() RETURNS TRIGGER AS $$
            DECLARE v_depth INTEGER;
            BEGIN
                IF NEW."ParentId" IS NOT NULL THEN
                    WITH RECURSIVE depth_cte AS (
                        SELECT "Id", "ParentId", 0 AS d FROM "BvhNodes" WHERE "Id" = NEW."ParentId"
                        UNION ALL
                        SELECT n."Id", n."ParentId", dc.d + 1
                        FROM "BvhNodes" n JOIN depth_cte dc ON n."Id" = dc."ParentId"
                        WHERE dc.d < 34
                    )
                    SELECT MAX(d) INTO v_depth FROM depth_cte;
                    IF v_depth >= 32 THEN
                        RAISE EXCEPTION 'bvh_max_depth_exceeded: depth % at node %', v_depth, NEW."Id";
                    END IF;
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER "TR_BvhNodes_DepthLimit"
                BEFORE INSERT ON "BvhNodes"
                FOR EACH ROW EXECUTE FUNCTION check_bvh_depth();
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_BvhNodes_PhysicalSpaceId"
            ON "BvhNodes" ("PhysicalSpaceId")
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_BvhNodes_ParentId"
            ON "BvhNodes" ("ParentId") WHERE "ParentId" IS NOT NULL
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_BvhNodes_ElementId"
            ON "BvhNodes" ("ElementId") WHERE "IsLeaf" = true
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_BvhNodes_ElementId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_BvhNodes_ParentId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_BvhNodes_PhysicalSpaceId\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS \"TR_BvhNodes_DepthLimit\" ON \"BvhNodes\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP FUNCTION IF EXISTS check_bvh_depth()",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP POLICY IF EXISTS \"bvh_tenant_isolation\" ON \"BvhNodes\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP TABLE IF EXISTS \"BvhNodes\"",
            suppressTransaction: true);
    }
}
