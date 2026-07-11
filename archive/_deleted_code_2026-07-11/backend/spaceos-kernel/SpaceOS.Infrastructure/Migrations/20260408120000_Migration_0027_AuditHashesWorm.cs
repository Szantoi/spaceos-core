// SpaceOS.Infrastructure/Migrations/20260408120000_Migration_0027_AuditHashesWorm.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0027 — Creates the <c>AuditHashes</c> WORM table for immutable audit hash chain storage.
/// <para>
/// The table is INSERT-only for the <c>spaceos_audit_worm</c> role (SEC-03).
/// RLS is enabled with a sentinel fallback to the SpaceOS system tenant (all-zeros UUID + 1).
/// </para>
/// </summary>
public partial class Migration_0027_AuditHashesWorm : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "AuditHashes" (
                "TenantId"   uuid        NOT NULL,
                "BlockIndex" bigint      NOT NULL,
                "Hash"       varchar(64) NOT NULL,
                "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
                CONSTRAINT "PK_AuditHashes" PRIMARY KEY ("TenantId", "BlockIndex")
            );
            ALTER TABLE "AuditHashes" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AuditHashes" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "rls_audit_hashes_tenant"
                ON "AuditHashes" USING (
                    "TenantId" = COALESCE(
                        NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid,
                        '00000000-0000-0000-0000-000000000001'::uuid));
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS "rls_audit_hashes_tenant" ON "AuditHashes";
            DROP TABLE IF EXISTS "AuditHashes";
            """,
            suppressTransaction: true);
    }
}
