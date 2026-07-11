using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <summary>
    /// Migration 0029 — Ecosystem Actor Architecture v4.
    /// <list type="bullet">
    /// <item>Adds <c>TenantType</c> varchar(32) column to <c>Tenants</c> with immutability trigger (SEC-01).</item>
    /// <item>Expands <c>EnabledModules</c> CHECK constraint to include the full v4 module set (SEC-02 companion trigger).</item>
    /// <item>Expands <c>AllowedTradeTypes</c> CHECK on <c>TenantHandshakeAllowlist</c>.</item>
    /// <item>Seeds demo tenants for all 5 non-Manufacturer actor types.</item>
    /// </list>
    /// </summary>
    public partial class Migration_0029_EcosystemActorTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add TenantType column
            migrationBuilder.Sql(
                """
                ALTER TABLE "Tenants"
                  ADD COLUMN IF NOT EXISTS "TenantType" varchar(32) NOT NULL DEFAULT 'Manufacturer';
                """,
                suppressTransaction: true);

            // 2. TenantType CHECK constraint
            migrationBuilder.Sql(
                """
                ALTER TABLE "Tenants"
                  ADD CONSTRAINT "CK_Tenants_TenantType_Valid"
                  CHECK ("TenantType" IN ('Manufacturer','PanelCutter','Trader','Logistics','Installer','EndCustomer'));
                """,
                suppressTransaction: true);

            // 3. Expand EnabledModules CHECK (atomic DO $$ swap)
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  ALTER TABLE "Tenants" DROP CONSTRAINT IF EXISTS "CK_Tenants_EnabledModules_Valid";
                  ALTER TABLE "Tenants" ADD CONSTRAINT "CK_Tenants_EnabledModules_Valid"
                    CHECK ("EnabledModules" <@ ARRAY['door','cabinet','window','cutting','spatial','trading','delivery','installation','orders']::varchar(32)[]);
                END $$;
                """,
                suppressTransaction: true);

            // 4. Expand AllowedTradeTypes CHECK on TenantHandshakeAllowlist
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  ALTER TABLE "TenantHandshakeAllowlist" DROP CONSTRAINT IF EXISTS "CK_TenantHandshakeAllowlist_TradeTypes";
                  ALTER TABLE "TenantHandshakeAllowlist" ADD CONSTRAINT "CK_TenantHandshakeAllowlist_TradeTypes"
                    CHECK ("AllowedTradeTypes" <@ ARRAY['door','cabinet','window','cutting','delivery','installation']::varchar(32)[]);
                END $$;
                """,
                suppressTransaction: true);

            // 5. Index on TenantType
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Tenants_TenantType" ON "Tenants" ("TenantType");
                """,
                suppressTransaction: true);

            // 6. Update Doorstar EnabledModules BEFORE triggers (Manufacturer should have 'door' seed)
            migrationBuilder.Sql(
                """
                UPDATE "Tenants" SET "EnabledModules" = ARRAY['door']
                WHERE "BrandSkinId" = 'doorstar' AND "EnabledModules" = '{}';
                """,
                suppressTransaction: true);

            // 7. TRIGGER: TenantType immutable (SEC-01)
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION prevent_tenant_type_change()
                RETURNS TRIGGER AS $$
                BEGIN
                  IF OLD."TenantType" IS DISTINCT FROM NEW."TenantType" THEN
                    RAISE EXCEPTION 'TenantType is immutable after creation. Current: %, Attempted: %',
                      OLD."TenantType", NEW."TenantType";
                  END IF;
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS "TR_Tenants_ImmutableTenantType" ON "Tenants";
                CREATE TRIGGER "TR_Tenants_ImmutableTenantType"
                  BEFORE UPDATE ON "Tenants"
                  FOR EACH ROW
                  EXECUTE FUNCTION prevent_tenant_type_change();
                """,
                suppressTransaction: true);

            // 8. TRIGGER: EnabledModules per-type validation (SEC-02)
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION validate_enabled_modules_for_type()
                RETURNS TRIGGER AS $$
                DECLARE allowed_modules varchar(32)[];
                BEGIN
                  CASE NEW."TenantType"
                    WHEN 'Manufacturer' THEN allowed_modules := ARRAY['door','cabinet','window','cutting','spatial'];
                    WHEN 'PanelCutter'  THEN allowed_modules := ARRAY['cutting'];
                    WHEN 'Trader'       THEN allowed_modules := ARRAY['trading','delivery'];
                    WHEN 'Logistics'    THEN allowed_modules := ARRAY['delivery'];
                    WHEN 'Installer'    THEN allowed_modules := ARRAY['installation'];
                    WHEN 'EndCustomer'  THEN allowed_modules := ARRAY['orders'];
                    ELSE RAISE EXCEPTION 'Unknown TenantType: %', NEW."TenantType";
                  END CASE;
                  IF NOT (NEW."EnabledModules" <@ allowed_modules) THEN
                    RAISE EXCEPTION 'EnabledModules % not allowed for TenantType %. Allowed: %',
                      NEW."EnabledModules", NEW."TenantType", allowed_modules;
                  END IF;
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS "TR_Tenants_ValidateModulesForType" ON "Tenants";
                CREATE TRIGGER "TR_Tenants_ValidateModulesForType"
                  BEFORE INSERT OR UPDATE ON "Tenants"
                  FOR EACH ROW
                  EXECUTE FUNCTION validate_enabled_modules_for_type();
                """,
                suppressTransaction: true);

            // 9. Demo seed — idempotent via ON CONFLICT
            migrationBuilder.Sql(
                """
                INSERT INTO "Tenants" ("Id","Name","TenantType","EnabledModules","IsArchived")
                VALUES
                  ('00000000-0000-0000-0000-000000000010','Demo Szekrénygyártó Bt.','Manufacturer',ARRAY['cabinet','cutting','spatial'],false),
                  ('00000000-0000-0000-0000-000000000011','Demo Lapszabász Kft.','PanelCutter',ARRAY['cutting'],false),
                  ('00000000-0000-0000-0000-000000000012','Demo Anyagkereskedő Kft.','Trader',ARRAY['trading'],false),
                  ('00000000-0000-0000-0000-000000000013','Demo Fuvarozó Kft.','Logistics',ARRAY['delivery'],false),
                  ('00000000-0000-0000-0000-000000000014','Demo Beszerelő Kft.','Installer',ARRAY['installation'],false)
                ON CONFLICT ("Id") DO UPDATE SET
                  "Name" = EXCLUDED."Name",
                  "EnabledModules" = EXCLUDED."EnabledModules";
                """,
                suppressTransaction: true);

            // 10. Demo B2B connections
            migrationBuilder.Sql(
                """
                INSERT INTO "TenantHandshakeAllowlist" ("GuestTenantId","HostTenantId","AllowedTradeTypes")
                VALUES
                  ('00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000011',ARRAY['cutting']),
                  ('00000000-0000-0000-0000-000000000012','00000000-0000-0000-0000-000000000013',ARRAY['delivery'])
                ON CONFLICT ("GuestTenantId","HostTenantId") DO UPDATE SET
                  "AllowedTradeTypes" = EXCLUDED."AllowedTradeTypes";
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_Tenants_ValidateModulesForType" ON "Tenants";
                DROP FUNCTION IF EXISTS validate_enabled_modules_for_type();

                DROP TRIGGER IF EXISTS "TR_Tenants_ImmutableTenantType" ON "Tenants";
                DROP FUNCTION IF EXISTS prevent_tenant_type_change();

                DROP INDEX IF EXISTS "IX_Tenants_TenantType";

                DO $$
                BEGIN
                  ALTER TABLE "TenantHandshakeAllowlist" DROP CONSTRAINT IF EXISTS "CK_TenantHandshakeAllowlist_TradeTypes";
                  ALTER TABLE "TenantHandshakeAllowlist" ADD CONSTRAINT "CK_TenantHandshakeAllowlist_TradeTypes"
                    CHECK ("AllowedTradeTypes" <@ ARRAY['door','cabinet','window']::varchar(32)[]);
                END $$;

                DO $$
                BEGIN
                  ALTER TABLE "Tenants" DROP CONSTRAINT IF EXISTS "CK_Tenants_EnabledModules_Valid";
                  ALTER TABLE "Tenants" ADD CONSTRAINT "CK_Tenants_EnabledModules_Valid"
                    CHECK ("EnabledModules" <@ ARRAY['door','cabinet','window']::varchar(32)[]);
                END $$;

                ALTER TABLE "Tenants" DROP CONSTRAINT IF EXISTS "CK_Tenants_TenantType_Valid";
                ALTER TABLE "Tenants" DROP COLUMN IF EXISTS "TenantType";
                """,
                suppressTransaction: true);
        }
    }
}
