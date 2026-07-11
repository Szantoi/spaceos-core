// SpaceOS.Infrastructure/Migrations/20260407170000_Migration_0022_ProofHashColumns.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0022 — Adds proof hash columns to proof-bearing entities.
/// <para>
/// NOTE: The <c>ImplementationSummaries</c> table referenced in the Sprint D Phase 3B
/// specification does not exist in this codebase. This migration is a structural
/// placeholder that reserves the migration number and documents the intent.
/// When <c>ImplementationSummary</c> is added to the domain, this migration should be
/// updated with the corresponding ALTER TABLE statements.
/// </para>
/// <para>
/// Adds <c>ProofHash</c>, <c>ProofStorageKey</c>, and <c>ProofStorageProvider</c> columns
/// to <c>FlowEpics</c> where applicable (FlowEpic already has <c>ProofHash</c> and
/// <c>ProofUrl</c> from Sprint C — no schema change required here).
/// </para>
/// </summary>
public partial class Migration_0022_ProofHashColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // FlowEpics.ProofHash was added in SprintC_SchemaUpdate.
        // FlowEpics.ProofUrl was added in SprintC_SchemaUpdate.
        // ImplementationSummaries table does not exist — no DDL to run.
        // This migration is intentionally empty for this deployment.
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Nothing to roll back.
    }
}
