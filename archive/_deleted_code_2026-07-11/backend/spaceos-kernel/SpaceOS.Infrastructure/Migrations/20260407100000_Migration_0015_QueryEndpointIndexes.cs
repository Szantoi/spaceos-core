// SpaceOS.Infrastructure/Migrations/20260407100000_Migration_0015_QueryEndpointIndexes.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0015 — Adds composite/partial indexes for the Tool Registry query endpoints (BE-P2-02).
/// <para>
/// CREATE INDEX CONCURRENTLY is used so that each index can be built without locking writes.
/// suppressTransaction must be true because CONCURRENTLY cannot run inside a transaction.
/// IF NOT EXISTS ensures idempotent runs.
/// </para>
/// </summary>
public partial class Migration_0015_QueryEndpointIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // FlowEpics: tenant-scoped non-archived list (used by ListFlowEpics endpoint)
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_FlowEpics_TenantId_IsArchived"
            ON "FlowEpics" ("TenantId")
            WHERE "IsArchived" = false
            """,
            suppressTransaction: true);

        // WorkStations: tenant-scoped non-archived list
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_WorkStations_TenantId_IsArchived"
            ON "WorkStations" ("TenantId")
            WHERE "IsArchived" = false
            """,
            suppressTransaction: true);

        // Facilities: tenant-scoped non-archived list
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Facilities_TenantId_IsArchived"
            ON "Facilities" ("TenantId")
            WHERE "IsArchived" = false
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_FlowEpics_TenantId_IsArchived\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_WorkStations_TenantId_IsArchived\"",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_Facilities_TenantId_IsArchived\"",
            suppressTransaction: true);
    }
}
