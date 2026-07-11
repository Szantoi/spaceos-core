// SpaceOS.Infrastructure/Migrations/20260408110000_Migration_0026_TenantHandshakeAllowlist.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0026 — Creates the <c>TenantHandshakeAllowlist</c> table for B2B cross-tenant
/// handshake authorization.
/// <para>
/// Includes composite primary key, foreign keys to <c>Tenants</c>, CHECK constraints for
/// no-self-link and valid trade types, RLS policies for tenant isolation, and seed data
/// linking "cabinetmaker" guest to "doorstar" host with trade type "door".
/// </para>
/// </summary>
public partial class Migration_0026_TenantHandshakeAllowlist : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "TenantHandshakeAllowlist" (
              "GuestTenantId"     uuid         NOT NULL,
              "HostTenantId"      uuid         NOT NULL,
              "AllowedTradeTypes" varchar(32)[] NOT NULL DEFAULT '{}',
              "CreatedAt"         timestamptz  NOT NULL DEFAULT now(),
              CONSTRAINT "PK_TenantHandshakeAllowlist" PRIMARY KEY ("GuestTenantId", "HostTenantId"),
              CONSTRAINT "FK_TenantHandshakeAllowlist_Guest" FOREIGN KEY ("GuestTenantId") REFERENCES "Tenants"("Id") ON DELETE CASCADE,
              CONSTRAINT "FK_TenantHandshakeAllowlist_Host" FOREIGN KEY ("HostTenantId") REFERENCES "Tenants"("Id") ON DELETE CASCADE,
              CONSTRAINT "CK_TenantHandshakeAllowlist_NoSelfLink" CHECK ("GuestTenantId" <> "HostTenantId"),
              CONSTRAINT "CK_TenantHandshakeAllowlist_TradeTypes" CHECK ("AllowedTradeTypes" <@ ARRAY['door','cabinet','window']::varchar(32)[]),
              CONSTRAINT "CK_AllowedTradeTypes_NotEmpty" CHECK (cardinality("AllowedTradeTypes") > 0)
            );
            CREATE INDEX IF NOT EXISTS "IX_TenantHandshakeAllowlist_Guest" ON "TenantHandshakeAllowlist" ("GuestTenantId");
            ALTER TABLE "TenantHandshakeAllowlist" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "TenantHandshakeAllowlist" FORCE ROW LEVEL SECURITY;
            CREATE POLICY "TenantHandshakeAllowlist_TenantIsolation" ON "TenantHandshakeAllowlist"
              USING ("GuestTenantId" = current_setting('app.current_tenant_id', true)::uuid
                     OR "HostTenantId" = current_setting('app.current_tenant_id', true)::uuid);
            INSERT INTO "TenantHandshakeAllowlist" ("GuestTenantId","HostTenantId","AllowedTradeTypes")
              SELECT g."Id", h."Id", ARRAY['door'] FROM "Tenants" g, "Tenants" h
              WHERE g."BrandSkinId" = 'cabinetmaker' AND h."BrandSkinId" = 'doorstar'
              ON CONFLICT ("GuestTenantId","HostTenantId") DO NOTHING;
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS "TenantHandshakeAllowlist_TenantIsolation" ON "TenantHandshakeAllowlist";
            DROP TABLE IF EXISTS "TenantHandshakeAllowlist";
            """,
            suppressTransaction: true);
    }
}
