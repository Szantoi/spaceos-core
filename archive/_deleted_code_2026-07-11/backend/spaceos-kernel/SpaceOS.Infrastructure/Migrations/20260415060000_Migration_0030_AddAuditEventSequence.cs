// SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0030 — Adds a monotone <c>Sequence</c> identity column to <c>AuditEvents</c>.
/// </summary>
/// <remarks>
/// The column is declared <c>GENERATED ALWAYS AS IDENTITY</c> so the database assigns a
/// strictly-increasing value on every insert, independent of clock resolution.
/// <para>
/// This fixes the non-deterministic ordering in <c>GetChainAsync</c> when two events share
/// the same <c>OccurredAt</c> millisecond tick under high concurrency (bulk import,
/// B2B handshake burst) — identified in MSG-KERNEL-074/R-15.
/// </para>
/// <para>
/// Production-safe: existing rows automatically receive a sequence value assigned in
/// insert order by PostgreSQL.  The <c>__EFMigrationsHistory</c> entry is written normally.
/// </para>
/// </remarks>
public partial class Migration_0030_AddAuditEventSequence : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "AuditEvents"
              ADD COLUMN "Sequence" BIGINT GENERATED ALWAYS AS IDENTITY;
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Sequence",
            table: "AuditEvents");
    }
}
