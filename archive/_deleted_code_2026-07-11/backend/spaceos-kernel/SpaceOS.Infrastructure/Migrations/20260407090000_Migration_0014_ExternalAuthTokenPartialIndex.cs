// SpaceOS.Infrastructure/Migrations/20260407090000_Migration_0014_ExternalAuthTokenPartialIndex.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0014 — Adds a partial index on <c>SpaceLayers.ExternalAuthTokenRef</c> (DB-02).
/// The index covers only rows where the column IS NOT NULL, optimising federated SpaceLayer queries.
/// <para>
/// CREATE INDEX CONCURRENTLY is used so that the index can be built without locking writes.
/// suppressTransaction must be true because CONCURRENTLY cannot run inside a transaction.
/// </para>
/// </summary>
public partial class Migration_0014_ExternalAuthTokenPartialIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DB-02: Partial index on ExternalAuthTokenRef for federated SpaceLayer queries.
        // suppressTransaction: true — CREATE INDEX CONCURRENTLY cannot run inside a transaction.
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SpaceLayers_ExternalAuthTokenRef_NotNull"
            ON "SpaceLayers" ("ExternalAuthTokenRef")
            WHERE "ExternalAuthTokenRef" IS NOT NULL
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS \"IX_SpaceLayers_ExternalAuthTokenRef_NotNull\"",
            suppressTransaction: true);
    }
}
