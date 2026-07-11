// SpaceOS.Infrastructure/Migrations/20260407160000_Migration_0021_OutboxMessagesPostgres.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0021 — PostgreSQL-specific DDL for OutboxMessages:
/// adds RLS policies, owner assignment, and a partial polling index.
/// The SQLite schema was created in SprintC_SchemaUpdate; this migration
/// is a no-op when running against SQLite (Development/Test).
/// </summary>
public partial class Migration_0021_OutboxMessagesPostgres : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS "OutboxMessages" OWNER TO spaceos_schema_owner;
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_policies
                    WHERE tablename = 'OutboxMessages' AND policyname = 'tenant_isolation'
                ) THEN
                    ALTER TABLE "OutboxMessages" ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE "OutboxMessages" FORCE ROW LEVEL SECURITY;
                    CREATE POLICY "tenant_isolation" ON "OutboxMessages"
                        USING ("TenantId" = current_setting('app.current_tenant_id')::uuid
                               OR current_setting('app.current_tenant_id')::uuid
                                    = '00000000-0000-0000-0000-000000000001'::uuid);
                END IF;
            END $$;
            """,
            suppressTransaction: true);

        // Partial polling index: only unprocessed messages need to be scanned by the worker.
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_OutboxMessages_Polling"
                ON "OutboxMessages" ("CreatedAt" ASC)
                WHERE "ProcessedAt" IS NULL;
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX CONCURRENTLY IF EXISTS "IX_OutboxMessages_Polling";
            """,
            suppressTransaction: true);
    }
}
