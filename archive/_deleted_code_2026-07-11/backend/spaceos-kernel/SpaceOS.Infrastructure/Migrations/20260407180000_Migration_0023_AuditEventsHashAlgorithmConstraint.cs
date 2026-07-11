// SpaceOS.Infrastructure/Migrations/20260407180000_Migration_0023_AuditEventsHashAlgorithmConstraint.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0023 — Adds a PostgreSQL CHECK constraint on <c>AuditEvents.HashAlgorithm</c>
/// to enforce that only known algorithm names are stored.
/// <para>
/// The <c>HashAlgorithm</c> column was added in <c>SprintC_SchemaUpdate</c>; this migration
/// adds the domain constraint that was deferred until the enum values were stabilised.
/// </para>
/// <para>
/// All DDL uses <c>suppressTransaction: true</c> so that PostgreSQL CONCURRENTLY index
/// creation and constraint validation do not block concurrent reads.
/// </para>
/// </summary>
public partial class Migration_0023_AuditEventsHashAlgorithmConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add a CHECK constraint to AuditEvents.HashAlgorithm (PostgreSQL only).
        // Uses DO $$ to guard against re-running on SQLite (Development/Test).
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE constraint_name = 'CK_AuditEvents_HashAlgorithm'
                      AND table_name       = 'AuditEvents'
                ) THEN
                    ALTER TABLE "AuditEvents"
                        ADD CONSTRAINT "CK_AuditEvents_HashAlgorithm"
                        CHECK ("HashAlgorithm" IN ('SHA256', 'SHA3_256'));
                END IF;
            END $$;
            """,
            suppressTransaction: true);

        // Add a partial index on AuditEvents for SHA3_256 records to support
        // incremental algorithm migration queries.
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AuditEvents_HashAlgorithm_SHA3"
                ON "AuditEvents" ("TenantId", "OccurredAt")
                WHERE "HashAlgorithm" = 'SHA3_256';
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX CONCURRENTLY IF EXISTS "IX_AuditEvents_HashAlgorithm_SHA3";
            """,
            suppressTransaction: true);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.table_constraints
                    WHERE constraint_name = 'CK_AuditEvents_HashAlgorithm'
                      AND table_name       = 'AuditEvents'
                ) THEN
                    ALTER TABLE "AuditEvents"
                        DROP CONSTRAINT "CK_AuditEvents_HashAlgorithm";
                END IF;
            END $$;
            """,
            suppressTransaction: true);
    }
}
