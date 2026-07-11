using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations;

/// <inheritdoc />
public partial class Migration_0001_InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"CREATE SCHEMA IF NOT EXISTS spaceos_joinery;");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""DoorOrders"" (
    ""Id""               uuid            NOT NULL,
    ""TenantId""         uuid            NOT NULL,
    ""FlowEpicId""       uuid            NOT NULL,
    ""ProjectId""        varchar(30)     NOT NULL,
    ""ProjectName""      varchar(200)    NULL,
    ""Status""           varchar(20)     NOT NULL,
    ""ClientName""       varchar(200)    NULL,
    ""ClientAddress""    varchar(500)    NULL,
    ""ClientPhone""      varchar(50)     NULL,
    ""DeliveryDate""     date            NULL,
    CONSTRAINT ""PK_DoorOrders"" PRIMARY KEY (""Id"")
);");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""DoorItems"" (
    ""Id""                   uuid            NOT NULL,
    ""OrderId""              uuid            NOT NULL,
    ""Sorszam""              varchar(50)     NOT NULL,
    ""Name""                 varchar(200)    NULL,
    ""Quantity""             integer         NOT NULL,
    ""DoorType""             varchar(30)     NOT NULL,
    ""OpeningDirection""     varchar(15)     NOT NULL,
    ""WallOpeningWidth""     numeric(10,2)   NOT NULL DEFAULT 0,
    ""DoorWidth""            numeric(10,2)   NOT NULL DEFAULT 0,
    ""WallOpeningHeight""    numeric(10,2)   NOT NULL DEFAULT 0,
    ""DoorHeight""           numeric(10,2)   NOT NULL DEFAULT 0,
    ""WallOpeningThickness"" numeric(10,2)   NOT NULL DEFAULT 0,
    ""DoorThickness""        numeric(10,2)   NOT NULL DEFAULT 0,
    ""FixSurfaceType""       varchar(30)     NULL,
    ""FixColor""             varchar(100)    NULL,
    ""FixColorCode""         varchar(50)     NULL,
    ""FixPattern""           varchar(100)    NULL,
    ""FixPatternType""       varchar(100)    NULL,
    ""FixPatternProfile""    varchar(100)    NULL,
    ""FixCoatingColor""      varchar(100)    NULL,
    ""FixHasBlende""         boolean         NULL,
    ""FixHasWallPanel""      boolean         NULL,
    ""MovSurfaceType""       varchar(30)     NULL,
    ""MovColor""             varchar(100)    NULL,
    ""MovColorCode""         varchar(50)     NULL,
    ""MovPattern""           varchar(100)    NULL,
    ""MovPatternType""       varchar(100)    NULL,
    ""MovPatternProfile""    varchar(100)    NULL,
    ""MovCoatingColor""      varchar(100)    NULL,
    ""MovHasBlende""         boolean         NULL,
    ""MovHasWallPanel""      boolean         NULL,
    ""GlazingType""          varchar(100)    NULL,
    ""GlazingColor""         varchar(100)    NULL,
    ""GlazingStyle""         varchar(100)    NULL,
    ""GlazingPattern""       varchar(100)    NULL,
    ""LockType""             varchar(100)    NULL,
    ""LockSize""             varchar(50)     NULL,
    ""StrikeType""           varchar(100)    NULL,
    ""HandleType""           varchar(100)    NULL,
    ""HandleColor""          varchar(100)    NULL,
    ""HandleKit""            varchar(100)    NULL,
    ""KeyholeDrilling""      boolean         NULL,
    ""AutoThreshold""        boolean         NULL,
    ""PanelTensioner""       boolean         NULL,
    ""HingeType""            varchar(100)    NULL,
    ""HingeCount""           integer         NULL,
    ""HingeSpacing""         varchar(50)     NULL,
    ""HingeColor""           varchar(100)    NULL,
    ""EdgeStripType""        varchar(100)    NULL,
    ""EdgeStripColor""       varchar(100)    NULL,
    ""SealType""             varchar(100)    NULL,
    ""SealColor""            varchar(100)    NULL,
    ""FrameMaterial""        varchar(100)    NULL,
    ""InsertMaterial""       varchar(100)    NULL,
    ""CladMaterial""         varchar(100)    NULL,
    ""FrameCoreMaterial""    varchar(100)    NULL,
    ""BlendeMaterial""       varchar(100)    NULL,
    ""CoatingMaterial""      varchar(100)    NULL,
    ""CncProcessing""        varchar(200)    NULL,
    ""PanelProcessing""      varchar(200)    NULL,
    ""FrameProcessing""      varchar(200)    NULL,
    ""ProcessingNote""       varchar(500)    NULL,
    CONSTRAINT ""PK_DoorItems"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_DoorItems_DoorOrders"" FOREIGN KEY (""OrderId"")
        REFERENCES spaceos_joinery.""DoorOrders"" (""Id"") ON DELETE RESTRICT
);");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""DoorTypeRules"" (
    ""DoorType""            varchar(30)     NOT NULL,
    ""AjtólapCount""        integer         NOT NULL DEFAULT 0,
    ""BkmWidthFixed""       numeric(10,2)   NOT NULL DEFAULT 0,
    ""BkmHeightFixed""      numeric(10,2)   NOT NULL DEFAULT 0,
    ""BkmWidthMoving""      numeric(10,2)   NOT NULL DEFAULT 0,
    ""BkmHeightMoving""     numeric(10,2)   NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_DoorTypeRules"" PRIMARY KEY (""DoorType"")
);");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""PartDimensionRules"" (
    ""Id""                      uuid            NOT NULL,
    ""DoorType""                varchar(30)     NOT NULL,
    ""ComponentName""           varchar(200)    NOT NULL,
    ""ComponentType""           varchar(100)    NOT NULL,
    ""Material""                varchar(100)    NULL,
    ""Thickness""               numeric(10,2)   NULL,
    ""Quantity""                integer         NOT NULL DEFAULT 1,
    ""WidthBase""               numeric(10,4)   NOT NULL DEFAULT 0,
    ""WidthMultiplierFactor""   numeric(10,4)   NOT NULL DEFAULT 0,
    ""LengthBase""              numeric(10,4)   NOT NULL DEFAULT 0,
    ""LengthMultiplierFactor""  numeric(10,4)   NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_PartDimensionRules"" PRIMARY KEY (""Id"")
);");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""ProcessTaskTemplates"" (
    ""TaskId""          varchar(50)     NOT NULL,
    ""ShortName""       varchar(100)    NOT NULL,
    ""Description""     varchar(500)    NULL,
    ""Department""      varchar(100)    NULL,
    ""UnitTimeSec""     integer         NOT NULL DEFAULT 0,
    ""Headcount""       integer         NOT NULL DEFAULT 1,
    ""ParentTaskId""    varchar(50)     NULL,
    CONSTRAINT ""PK_ProcessTaskTemplates"" PRIMARY KEY (""TaskId"")
);");

        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""GlobalConstants"" (
    ""Key""     varchar(100)    NOT NULL,
    ""Value""   numeric(18,6)   NOT NULL,
    CONSTRAINT ""PK_GlobalConstants"" PRIMARY KEY (""Key"")
);");

        // Indexes
        migrationBuilder.Sql(@"CREATE INDEX ""IX_DoorOrders_TenantId"" ON spaceos_joinery.""DoorOrders"" (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_DoorOrders_FlowEpicId"" ON spaceos_joinery.""DoorOrders"" (""FlowEpicId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_DoorItems_OrderId"" ON spaceos_joinery.""DoorItems"" (""OrderId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_PartDimensionRules_DoorType"" ON spaceos_joinery.""PartDimensionRules"" (""DoorType"");");

        // RLS
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorOrders"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorOrders"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""DoorOrders""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");

        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorItems"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""DoorItems"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""DoorItems""
    USING (""OrderId"" IN (
        SELECT ""Id"" FROM spaceos_joinery.""DoorOrders""
        WHERE ""TenantId"" = current_setting('app.tenant_id', true)::uuid
    ));");

        // GlobalConstants permissions — read-only for app user
        migrationBuilder.Sql(@"REVOKE INSERT, UPDATE, DELETE ON spaceos_joinery.""GlobalConstants"" FROM spaceos;");
        migrationBuilder.Sql(@"GRANT SELECT ON spaceos_joinery.""GlobalConstants"" TO spaceos;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""DoorItems"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""DoorOrders"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""DoorTypeRules"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""PartDimensionRules"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""ProcessTaskTemplates"";");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""GlobalConstants"";");
        migrationBuilder.Sql(@"DROP SCHEMA IF EXISTS spaceos_joinery;");
    }
}
