// SpaceOS.Infrastructure/Migrations/20260410130000_Migration_0028_StageRegistry.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Migration_0028_StageRegistry : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- ============================================================
            -- Migration 0028 — Stage Registry + StageChain + StageHandoff
            -- ============================================================

            -- 1. StageDefinitions
            CREATE TABLE IF NOT EXISTS "StageDefinitions" (
                "Id"              uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
                "TenantId"        uuid          NOT NULL REFERENCES "Tenants"("Id"),
                "StageCode"       varchar(30)   NOT NULL
                    CHECK ("StageCode" ~ '^[a-z][a-z0-9_]{1,28}[a-z0-9]$'),
                "DisplayName"     varchar(100)  NOT NULL,
                "ModuleEndpoint"  varchar(500)  NOT NULL
                    CHECK ("ModuleEndpoint" ~ '^https?://(127\.0\.0\.1|localhost):(50[0-9]{2})$'),
                "IsActive"        boolean       NOT NULL DEFAULT true,
                "CreatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
                "UpdatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
                UNIQUE ("TenantId", "StageCode")
            );

            -- 2. StageChainTemplates
            CREATE TABLE IF NOT EXISTS "StageChainTemplates" (
                "Id"          uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
                "TenantId"    uuid          NOT NULL REFERENCES "Tenants"("Id"),
                "Name"        varchar(100)  NOT NULL,
                "IsDefault"   boolean       NOT NULL DEFAULT false,
                "CreatedAt"   timestamptz   NOT NULL DEFAULT NOW(),
                "UpdatedAt"   timestamptz   NOT NULL DEFAULT NOW(),
                UNIQUE ("TenantId", "Name")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_StageChainTemplates_DefaultPerTenant"
                ON "StageChainTemplates" ("TenantId") WHERE "IsDefault" = true;

            -- 3. StageChainSteps
            CREATE TABLE IF NOT EXISTS "StageChainSteps" (
                "Id"                  uuid        NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
                "TenantId"            uuid        NOT NULL,
                "ChainTemplateId"     uuid        NOT NULL
                    REFERENCES "StageChainTemplates"("Id") ON DELETE CASCADE,
                "StageDefinitionId"   uuid        NOT NULL
                    REFERENCES "StageDefinitions"("Id"),
                "StageCode"           varchar(30) NOT NULL,
                "SortOrder"           int         NOT NULL CHECK ("SortOrder" > 0),
                "IsOptional"          boolean     NOT NULL DEFAULT false,
                UNIQUE ("ChainTemplateId", "StageCode"),
                UNIQUE ("ChainTemplateId", "SortOrder")
            );

            -- 4. StageHandoffs
            CREATE TABLE IF NOT EXISTS "StageHandoffs" (
                "Id"                uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
                "TenantId"          uuid          NOT NULL,
                "FlowEpicId"        uuid          NOT NULL REFERENCES "FlowEpics"("Id"),
                "SourceStageCode"   varchar(30)   NOT NULL,
                "TargetStageCode"   varchar(30)   NOT NULL,
                "Version"           int           NOT NULL DEFAULT 1 CHECK ("Version" > 0),
                "IdempotencyKey"    uuid          NOT NULL,
                "PayloadJson"       jsonb         NOT NULL
                    CHECK (pg_column_size("PayloadJson") < 1048576),
                "PayloadHash"       varchar(64)   NOT NULL,
                "HashAlgorithm"     varchar(20)   NOT NULL DEFAULT 'SHA-256',
                "SourceActorId"     uuid          DEFAULT NULL REFERENCES "Tenants"("Id"),
                "TargetActorId"     uuid          DEFAULT NULL REFERENCES "Tenants"("Id"),
                "HandshakeId"       uuid          DEFAULT NULL,
                "CreatedAt"         timestamptz   NOT NULL DEFAULT NOW(),
                UNIQUE ("FlowEpicId", "SourceStageCode", "TargetStageCode", "Version"),
                UNIQUE ("FlowEpicId", "IdempotencyKey"),
                CHECK ("SourceStageCode" <> "TargetStageCode")
            );

            -- 5. FlowEpics extension
            ALTER TABLE "FlowEpics"
                ADD COLUMN IF NOT EXISTS "CurrentStageCode" varchar(30) DEFAULT NULL,
                ADD COLUMN IF NOT EXISTS "StageChainTemplateId" uuid DEFAULT NULL
                    REFERENCES "StageChainTemplates"("Id");

            ALTER TABLE "FlowEpics"
                DROP CONSTRAINT IF EXISTS "FK_FlowEpics_CurrentStage";

            ALTER TABLE "FlowEpics"
                ADD CONSTRAINT "FK_FlowEpics_CurrentStage"
                FOREIGN KEY ("TenantId", "CurrentStageCode")
                REFERENCES "StageDefinitions"("TenantId", "StageCode")
                DEFERRABLE INITIALLY DEFERRED;

            -- 4.2 Indexes
            CREATE INDEX IF NOT EXISTS "IX_StageDefinitions_TenantId"
                ON "StageDefinitions" ("TenantId");
            CREATE INDEX IF NOT EXISTS "IX_StageDefinitions_TenantId_Active"
                ON "StageDefinitions" ("TenantId", "StageCode") WHERE "IsActive" = true;
            CREATE INDEX IF NOT EXISTS "IX_StageChainSteps_ChainTemplateId"
                ON "StageChainSteps" ("ChainTemplateId");
            CREATE INDEX IF NOT EXISTS "IX_StageChainSteps_StageDefinitionId"
                ON "StageChainSteps" ("StageDefinitionId");
            CREATE INDEX IF NOT EXISTS "IX_StageHandoffs_FlowEpicId"
                ON "StageHandoffs" ("FlowEpicId");
            CREATE INDEX IF NOT EXISTS "IX_StageHandoffs_TenantId_Source"
                ON "StageHandoffs" ("TenantId", "SourceStageCode");
            CREATE INDEX IF NOT EXISTS "IX_StageHandoffs_TenantId_Target"
                ON "StageHandoffs" ("TenantId", "TargetStageCode");
            CREATE INDEX IF NOT EXISTS "IX_FlowEpics_CurrentStageCode"
                ON "FlowEpics" ("CurrentStageCode") WHERE "CurrentStageCode" IS NOT NULL;
            CREATE INDEX IF NOT EXISTS "IX_FlowEpics_StageChainTemplateId"
                ON "FlowEpics" ("StageChainTemplateId") WHERE "StageChainTemplateId" IS NOT NULL;

            -- 4.3 RLS (COALESCE sentinel pattern — same as existing migrations)
            ALTER TABLE "StageDefinitions" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "StageDefinitions" FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS "sd_tenant" ON "StageDefinitions";
            CREATE POLICY "sd_tenant" ON "StageDefinitions"
                USING ("TenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
                    '00000000-0000-0000-0000-000000000001'::uuid));

            ALTER TABLE "StageChainTemplates" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "StageChainTemplates" FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS "sct_tenant" ON "StageChainTemplates";
            CREATE POLICY "sct_tenant" ON "StageChainTemplates"
                USING ("TenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
                    '00000000-0000-0000-0000-000000000001'::uuid));

            ALTER TABLE "StageChainSteps" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "StageChainSteps" FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS "scs_tenant" ON "StageChainSteps";
            CREATE POLICY "scs_tenant" ON "StageChainSteps"
                USING ("TenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
                    '00000000-0000-0000-0000-000000000001'::uuid));

            ALTER TABLE "StageHandoffs" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "StageHandoffs" FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS "sh_tenant" ON "StageHandoffs";
            CREATE POLICY "sh_tenant" ON "StageHandoffs"
                USING ("TenantId" = COALESCE(NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
                    '00000000-0000-0000-0000-000000000001'::uuid));

            -- 4.4 Triggers
            CREATE OR REPLACE FUNCTION prevent_stage_code_change()
            RETURNS TRIGGER AS $$
            BEGIN
                IF NEW."StageCode" <> OLD."StageCode" THEN
                    RAISE EXCEPTION 'StageCode is immutable (was: %, new: %)', OLD."StageCode", NEW."StageCode";
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS "TR_StageDefinitions_ImmutableCode" ON "StageDefinitions";
            CREATE TRIGGER "TR_StageDefinitions_ImmutableCode"
                BEFORE UPDATE ON "StageDefinitions"
                FOR EACH ROW EXECUTE FUNCTION prevent_stage_code_change();

            CREATE OR REPLACE FUNCTION update_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW."UpdatedAt" = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS "TR_StageDefinitions_UpdatedAt" ON "StageDefinitions";
            CREATE TRIGGER "TR_StageDefinitions_UpdatedAt"
                BEFORE UPDATE ON "StageDefinitions"
                FOR EACH ROW EXECUTE FUNCTION update_updated_at();

            DROP TRIGGER IF EXISTS "TR_StageChainTemplates_UpdatedAt" ON "StageChainTemplates";
            CREATE TRIGGER "TR_StageChainTemplates_UpdatedAt"
                BEFORE UPDATE ON "StageChainTemplates"
                FOR EACH ROW EXECUTE FUNCTION update_updated_at();

            -- 4.5 Seed — Doorstar
            WITH doorstar AS (
                SELECT "Id" AS tid FROM "Tenants" WHERE "BrandSkinId" = 'doorstar' LIMIT 1
            )
            INSERT INTO "StageDefinitions" ("TenantId", "StageCode", "DisplayName", "ModuleEndpoint")
            SELECT d.tid, s.code, s.name, s.endpoint FROM doorstar d
            CROSS JOIN (VALUES
                ('sales',         'Értékesítés',  'http://127.0.0.1:5004'),
                ('survey',        'Felmérés',     'http://127.0.0.1:5005'),
                ('manufacturing', 'Gyártás',      'http://127.0.0.1:5002')
            ) AS s(code, name, endpoint)
            ON CONFLICT ("TenantId", "StageCode") DO NOTHING;

            WITH doorstar AS (
                SELECT "Id" AS tid FROM "Tenants" WHERE "BrandSkinId" = 'doorstar' LIMIT 1
            )
            INSERT INTO "StageChainTemplates" ("TenantId", "Name", "IsDefault")
            SELECT tid, 'standard', true FROM doorstar
            ON CONFLICT ("TenantId", "Name") DO NOTHING;

            WITH ctx AS (
                SELECT ct."Id" AS cid, ct."TenantId" AS tid
                FROM "StageChainTemplates" ct
                JOIN "Tenants" t ON t."Id" = ct."TenantId"
                WHERE t."BrandSkinId" = 'doorstar' AND ct."Name" = 'standard' LIMIT 1
            )
            INSERT INTO "StageChainSteps" ("TenantId", "ChainTemplateId", "StageDefinitionId", "StageCode", "SortOrder", "IsOptional")
            SELECT c.tid, c.cid, sd."Id", s.code, s.ord, s.opt
            FROM ctx c
            CROSS JOIN (VALUES ('sales',1,false), ('survey',2,true), ('manufacturing',3,false)) AS s(code,ord,opt)
            JOIN "StageDefinitions" sd ON sd."TenantId" = c.tid AND sd."StageCode" = s.code
            ON CONFLICT ("ChainTemplateId", "StageCode") DO NOTHING;
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "FlowEpics" DROP CONSTRAINT IF EXISTS "FK_FlowEpics_CurrentStage";
            ALTER TABLE "FlowEpics" DROP COLUMN IF EXISTS "CurrentStageCode";
            ALTER TABLE "FlowEpics" DROP COLUMN IF EXISTS "StageChainTemplateId";
            DROP TABLE IF EXISTS "StageHandoffs";
            DROP TABLE IF EXISTS "StageChainSteps";
            DROP TABLE IF EXISTS "StageChainTemplates";
            DROP TABLE IF EXISTS "StageDefinitions";
            DROP FUNCTION IF EXISTS prevent_stage_code_change();
            DROP FUNCTION IF EXISTS update_updated_at();
            """, suppressTransaction: true);
    }
}
