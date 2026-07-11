using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations;

/// <inheritdoc />
public partial class J0002_V2_CuttingListSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── DoorOrders: new columns ──────────────────────────────────────────────

        migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrders""
    ADD COLUMN ""CalculationError"" varchar(2000) NULL,
    ADD COLUMN ""Version"" integer NOT NULL DEFAULT 1;");

        // Extend Status CHECK constraint to include the three new states.
        // Drop-and-recreate because PostgreSQL does not support ALTER CONSTRAINT.
        migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_schema = 'spaceos_joinery'
          AND table_name = 'DoorOrders'
          AND constraint_type = 'CHECK'
          AND constraint_name LIKE '%Status%'
    ) THEN
        ALTER TABLE spaceos_joinery.""DoorOrders"" DROP CONSTRAINT IF EXISTS ""CK_DoorOrders_Status"";
    END IF;
END$$;

ALTER TABLE spaceos_joinery.""DoorOrders""
    ADD CONSTRAINT ""CK_DoorOrders_Status"" CHECK (
        ""Status"" IN (
            'Draft','Submitted',
            'Calculating','Calculated','CalculationFailed',
            'InProduction','Completed','Cancelled'
        )
    );");

        // ── CuttingListSnapshots ─────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""CuttingListSnapshots"" (
    ""Id""                       uuid            NOT NULL,
    ""TenantId""                 uuid            NOT NULL,
    ""DoorOrderId""              uuid            NOT NULL,
    ""DoorItemId""               uuid            NOT NULL,
    ""TemplateName""             varchar(200)    NOT NULL,
    ""TemplateVersion""          integer         NOT NULL,
    ""InputWidth""               numeric(8,2)    NOT NULL
        CONSTRAINT ""CK_CuttingListSnapshots_InputWidth"" CHECK (""InputWidth"" > 0 AND ""InputWidth"" <= 10000),
    ""InputHeight""              numeric(8,2)    NOT NULL
        CONSTRAINT ""CK_CuttingListSnapshots_InputHeight"" CHECK (""InputHeight"" > 0 AND ""InputHeight"" <= 10000),
    ""ParameterOverridesJson""   jsonb           NOT NULL DEFAULT '{}',
    ""ContentHash""              varchar(64)     NOT NULL,
    ""CalculatedAt""             timestamptz     NOT NULL,
    ""IsLatest""                 boolean         NOT NULL DEFAULT true,
    CONSTRAINT ""PK_CuttingListSnapshots"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_CuttingListSnapshots_DoorOrders"" FOREIGN KEY (""DoorOrderId"")
        REFERENCES spaceos_joinery.""DoorOrders"" (""Id"") ON DELETE RESTRICT,
    CONSTRAINT ""FK_CuttingListSnapshots_DoorItems"" FOREIGN KEY (""DoorItemId"")
        REFERENCES spaceos_joinery.""DoorItems"" (""Id"") ON DELETE RESTRICT
);");

        migrationBuilder.Sql(@"CREATE INDEX ""IX_CuttingListSnapshots_DoorOrderId"" ON spaceos_joinery.""CuttingListSnapshots"" (""DoorOrderId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_CuttingListSnapshots_DoorItemId""  ON spaceos_joinery.""CuttingListSnapshots"" (""DoorItemId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_CuttingListSnapshots_TenantId""    ON spaceos_joinery.""CuttingListSnapshots"" (""TenantId"");");

        // DB-03: partial unique index — only one IsLatest=true row per DoorItemId
        migrationBuilder.Sql(@"
CREATE UNIQUE INDEX ""UX_CuttingListSnapshots_DoorItemId_Latest""
    ON spaceos_joinery.""CuttingListSnapshots"" (""DoorItemId"")
    WHERE ""IsLatest"" = true;");

        // RLS
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CuttingListSnapshots"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CuttingListSnapshots"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""CuttingListSnapshots""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // ── CuttingListLines ─────────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""CuttingListLines"" (
    ""Id""              uuid            NOT NULL,
    ""SnapshotId""      uuid            NOT NULL,
    ""TenantId""        uuid            NOT NULL,
    ""ComponentName""   varchar(100)    NOT NULL,
    ""ComponentType""   varchar(50)     NOT NULL,
    ""Width""           numeric(8,2)    NOT NULL CONSTRAINT ""CK_CuttingListLines_Width""         CHECK (""Width""         > 0),
    ""Height""          numeric(8,2)    NOT NULL CONSTRAINT ""CK_CuttingListLines_Height""        CHECK (""Height""        > 0),
    ""CuttingWidth""    numeric(8,2)    NOT NULL CONSTRAINT ""CK_CuttingListLines_CuttingWidth""  CHECK (""CuttingWidth""  > 0),
    ""CuttingHeight""   numeric(8,2)    NOT NULL CONSTRAINT ""CK_CuttingListLines_CuttingHeight"" CHECK (""CuttingHeight"" > 0),
    ""Material""        varchar(100)    NOT NULL,
    ""Thickness""       numeric(6,2)    NOT NULL CONSTRAINT ""CK_CuttingListLines_Thickness""     CHECK (""Thickness""     > 0),
    ""Quantity""        integer         NOT NULL CONSTRAINT ""CK_CuttingListLines_Quantity""      CHECK (""Quantity"" > 0 AND ""Quantity"" <= 100),
    ""SortOrder""       integer         NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_CuttingListLines"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_CuttingListLines_CuttingListSnapshots"" FOREIGN KEY (""SnapshotId"")
        REFERENCES spaceos_joinery.""CuttingListSnapshots"" (""Id"") ON DELETE CASCADE
);");

        migrationBuilder.Sql(@"CREATE INDEX ""IX_CuttingListLines_SnapshotId"" ON spaceos_joinery.""CuttingListLines"" (""SnapshotId"");");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CuttingListLines"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CuttingListLines"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""CuttingListLines""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // ── CncInstructions ──────────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""CncInstructions"" (
    ""Id""              uuid            NOT NULL,
    ""SnapshotId""      uuid            NOT NULL,
    ""TenantId""        uuid            NOT NULL,
    ""ComponentName""   varchar(100)    NOT NULL,
    ""Operation""       varchar(30)     NOT NULL
        CONSTRAINT ""CK_CncInstructions_Operation"" CHECK (
            ""Operation"" IN ('None','Cut','AngledCut','Groove','Drill','EdgeBand','Chamfer','Round','Pocket','Profile')
        ),
    ""Position""        varchar(200)    NULL,
    ""Diameter""        numeric(6,2)    NULL,
    ""Depth""           numeric(6,2)    NULL,
    ""Angle""           numeric(6,2)    NULL,
    ""Note""            varchar(500)    NULL,
    CONSTRAINT ""PK_CncInstructions"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_CncInstructions_CuttingListSnapshots"" FOREIGN KEY (""SnapshotId"")
        REFERENCES spaceos_joinery.""CuttingListSnapshots"" (""Id"") ON DELETE CASCADE
);");

        migrationBuilder.Sql(@"CREATE INDEX ""IX_CncInstructions_SnapshotId"" ON spaceos_joinery.""CncInstructions"" (""SnapshotId"");");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CncInstructions"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""CncInstructions"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""CncInstructions""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // ── ProcessSteps ─────────────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""ProcessSteps"" (
    ""Id""                  uuid            NOT NULL,
    ""SnapshotId""          uuid            NOT NULL,
    ""TenantId""            uuid            NOT NULL,
    ""Phase""               varchar(30)     NOT NULL
        CONSTRAINT ""CK_ProcessSteps_Phase"" CHECK (
            ""Phase"" IN ('Design','Cutting','CNC','EdgeBanding','Surface','Assembly','QualityControl','Packaging')
        ),
    ""StepOrder""           integer         NOT NULL,
    ""Description""         varchar(500)    NULL,
    ""EstimatedSeconds""    integer         NOT NULL CONSTRAINT ""CK_ProcessSteps_EstimatedSeconds"" CHECK (""EstimatedSeconds"" >= 0),
    CONSTRAINT ""PK_ProcessSteps"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_ProcessSteps_CuttingListSnapshots"" FOREIGN KEY (""SnapshotId"")
        REFERENCES spaceos_joinery.""CuttingListSnapshots"" (""Id"") ON DELETE CASCADE
);");

        migrationBuilder.Sql(@"CREATE INDEX ""IX_ProcessSteps_SnapshotId"" ON spaceos_joinery.""ProcessSteps"" (""SnapshotId"");");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProcessSteps"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProcessSteps"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""ProcessSteps""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // ── ProductionSheetCache ─────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""ProductionSheetCache"" (
    ""Id""              uuid            NOT NULL,
    ""TenantId""        uuid            NOT NULL,
    ""SnapshotId""      uuid            NOT NULL,
    ""FilePath""        varchar(500)    NOT NULL,
    ""FileHash""        varchar(64)     NOT NULL,
    ""GeneratedAt""     timestamptz     NOT NULL,
    CONSTRAINT ""PK_ProductionSheetCache"" PRIMARY KEY (""Id""),
    CONSTRAINT ""UX_ProductionSheetCache_SnapshotId"" UNIQUE (""SnapshotId""),
    CONSTRAINT ""FK_ProductionSheetCache_CuttingListSnapshots"" FOREIGN KEY (""SnapshotId"")
        REFERENCES spaceos_joinery.""CuttingListSnapshots"" (""Id"") ON DELETE RESTRICT,
    CONSTRAINT ""CK_ProductionSheetCache_FilePath"" CHECK (""FilePath"" NOT LIKE '%..%')
);");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProductionSheetCache"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""ProductionSheetCache"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""ProductionSheetCache""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        // ── JoineryOutboxEntries ─────────────────────────────────────────────────

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""JoineryOutboxEntries"" (
    ""Id""              uuid            NOT NULL,
    ""TenantId""        uuid            NOT NULL,
    ""EventType""       varchar(200)    NOT NULL,
    ""PayloadJson""     jsonb           NOT NULL,
    ""CreatedAt""       timestamptz     NOT NULL DEFAULT NOW(),
    ""ProcessedAt""     timestamptz     NULL,
    ""FailedAt""        timestamptz     NULL,
    ""Error""           varchar(2000)   NULL,
    ""RetryCount""      integer         NOT NULL DEFAULT 0
        CONSTRAINT ""CK_JoineryOutboxEntries_RetryCount"" CHECK (""RetryCount"" >= 0 AND ""RetryCount"" <= 5),
    CONSTRAINT ""PK_JoineryOutboxEntries"" PRIMARY KEY (""Id"")
);");

        // Partial index for the background worker to efficiently poll pending entries
        migrationBuilder.Sql(@"
CREATE INDEX ""IX_JoineryOutboxEntries_Pending""
    ON spaceos_joinery.""JoineryOutboxEntries"" (""CreatedAt"")
    WHERE ""ProcessedAt"" IS NULL
      AND ""FailedAt""    IS NULL
      AND ""RetryCount""  < 3;");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""JoineryOutboxEntries"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""JoineryOutboxEntries"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""JoineryOutboxEntries""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""JoineryOutboxEntries"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""ProductionSheetCache"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""ProcessSteps"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""CncInstructions"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""CuttingListLines"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""CuttingListSnapshots"";");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorOrders"" DROP COLUMN IF EXISTS ""CalculationError"";");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorOrders"" DROP COLUMN IF EXISTS ""Version"";");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorOrders"" DROP CONSTRAINT IF EXISTS ""CK_DoorOrders_Status"";");
    }
}
