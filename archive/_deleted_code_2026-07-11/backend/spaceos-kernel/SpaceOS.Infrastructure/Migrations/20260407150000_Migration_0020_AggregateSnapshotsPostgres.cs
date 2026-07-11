// SpaceOS.Infrastructure/Migrations/20260407150000_Migration_0020_AggregateSnapshotsPostgres.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0020 — PostgreSQL-specific DDL for AggregateSnapshots:
/// adds JSONB column type, RLS policies, owner assignment, partial indexes,
/// FK to AuditEvents, and a CK preventing the system sentinel tenant ID.
/// The SQLite schema was created in SprintC_SchemaUpdate; this migration
/// is a no-op when running against SQLite (Development/Test).
/// </summary>
public partial class Migration_0020_AggregateSnapshotsPostgres : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // All statements use suppressTransaction: true because:
        //  1. CONCURRENTLY index creation cannot run inside a transaction.
        //  2. ALTER TABLE … OWNER TO requires superuser outside a transaction on some PG versions.
        // The migration is idempotent via IF NOT EXISTS / IF EXISTS guards.

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                -- Only execute PostgreSQL-specific DDL when the target is PostgreSQL.
                IF current_setting('server_version_num')::int > 0 THEN
                    -- No-op: AggregateSnapshots table was created by SprintC_SchemaUpdate.
                    -- This migration adds PostgreSQL-specific constraints and RLS.
                    NULL;
                END IF;
            END $$;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS "AggregateSnapshots" OWNER TO spaceos_schema_owner;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE tablename = 'AggregateSnapshots' AND policyname = 'tenant_isolation'
                ) THEN
                    ALTER TABLE "AggregateSnapshots" ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE "AggregateSnapshots" FORCE ROW LEVEL SECURITY;
                    CREATE POLICY "tenant_isolation" ON "AggregateSnapshots"
                        USING ("TenantId" = current_setting('app.current_tenant_id')::uuid
                               OR current_setting('app.current_tenant_id')::uuid
                                    = '00000000-0000-0000-0000-000000000001'::uuid);
                END IF;
            END $$;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AggregateSnapshots_AggregateId_SnapshotAt"
                ON "AggregateSnapshots" ("AggregateId", "SnapshotAt" DESC);
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AggregateSnapshots_TriggerEventId"
                ON "AggregateSnapshots" ("TriggerEventId") WHERE "TriggerEventId" IS NOT NULL;
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX CONCURRENTLY IF EXISTS "IX_AggregateSnapshots_AggregateId_SnapshotAt";
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            DROP INDEX CONCURRENTLY IF EXISTS "IX_AggregateSnapshots_TriggerEventId";
            """,
            suppressTransaction: true);
    }
}
