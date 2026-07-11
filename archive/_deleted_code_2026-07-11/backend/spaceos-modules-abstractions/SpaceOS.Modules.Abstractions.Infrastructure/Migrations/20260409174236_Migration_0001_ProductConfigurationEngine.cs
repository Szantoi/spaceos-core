using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Abstractions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Migration_0001_ProductConfigurationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "spaceos_modules");

            migrationBuilder.CreateTable(
                name: "GeometryAttachments",
                schema: "spaceos_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "varchar(20)", nullable: false),
                    SpatialElementId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkeletonJson = table.Column<string>(type: "jsonb", nullable: true),
                    FileReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileFormat = table.Column<string>(type: "varchar(10)", nullable: true),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeometryAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductTemplates",
                schema: "spaceos_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeType = table.Column<string>(type: "varchar(30)", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentSlots",
                schema: "spaceos_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComponentType = table.Column<string>(type: "varchar(50)", nullable: false),
                    SemanticRole = table.Column<string>(type: "varchar(20)", nullable: true),
                    DefaultMaterial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultThickness = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentSlots_ProductTemplates_ProductTemplateId",
                        column: x => x.ProductTemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComponentSlots_ProductTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlotConnections",
                schema: "spaceos_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Axis = table.Column<string>(type: "varchar(10)", nullable: false),
                    Operator = table.Column<string>(type: "varchar(20)", nullable: false),
                    Operand = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    MultiplierCount = table.Column<int>(type: "integer", nullable: true),
                    SecondaryParentSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    JointType = table.Column<string>(type: "varchar(30)", nullable: false),
                    MachiningOp = table.Column<string>(type: "varchar(20)", nullable: false),
                    ProcessPhase = table.Column<string>(type: "varchar(20)", nullable: false),
                    GrooveDepth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    GrooveWidth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    DrillDiameter = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    DrillDepth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Angle = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    Radius = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    JointNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotConnections_ProductTemplates_ProductTemplateId",
                        column: x => x.ProductTemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlotConnections_ProductTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateParameters",
                schema: "spaceos_modules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProductTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateParameters_ProductTemplates_ProductTemplateId",
                        column: x => x.ProductTemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TemplateParameters_ProductTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "spaceos_modules",
                        principalTable: "ProductTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComponentSlots_ProductTemplateId",
                schema: "spaceos_modules",
                table: "ComponentSlots",
                column: "ProductTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentSlots_TemplateId",
                schema: "spaceos_modules",
                table: "ComponentSlots",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryAttachments_SlotInstanceId",
                schema: "spaceos_modules",
                table: "GeometryAttachments",
                column: "SlotInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeometryAttachments_SpatialElementId",
                schema: "spaceos_modules",
                table: "GeometryAttachments",
                column: "SpatialElementId",
                filter: "\"SpatialElementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTemplates_TenantId",
                schema: "spaceos_modules",
                table: "ProductTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTemplates_TenantId_Name_Version",
                schema: "spaceos_modules",
                table: "ProductTemplates",
                columns: new[] { "TenantId", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductTemplates_TenantId_TradeType",
                schema: "spaceos_modules",
                table: "ProductTemplates",
                columns: new[] { "TenantId", "TradeType" },
                filter: "\"IsActive\" = true AND \"IsArchived\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SlotConnections_ChildSlotId",
                schema: "spaceos_modules",
                table: "SlotConnections",
                column: "ChildSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotConnections_ParentSlotId",
                schema: "spaceos_modules",
                table: "SlotConnections",
                column: "ParentSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotConnections_ProductTemplateId",
                schema: "spaceos_modules",
                table: "SlotConnections",
                column: "ProductTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotConnections_TemplateId",
                schema: "spaceos_modules",
                table: "SlotConnections",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotConnections_TemplateId_ParentSlotId_ChildSlotId_Axis",
                schema: "spaceos_modules",
                table: "SlotConnections",
                columns: new[] { "TemplateId", "ParentSlotId", "ChildSlotId", "Axis" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateParameters_ProductTemplateId",
                schema: "spaceos_modules",
                table: "TemplateParameters",
                column: "ProductTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateParameters_TemplateId",
                schema: "spaceos_modules",
                table: "TemplateParameters",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateParameters_TemplateId_Key",
                schema: "spaceos_modules",
                table: "TemplateParameters",
                columns: new[] { "TemplateId", "Key" },
                unique: true);

            // RLS — SEC-01: tenant isolation on all 5 tables
            migrationBuilder.Sql(@"
                ALTER TABLE spaceos_modules.""ProductTemplates"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE spaceos_modules.""ProductTemplates"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""pt_tenant"" ON spaceos_modules.""ProductTemplates""
                    USING (""TenantId"" = current_setting('app.tenant_id')::uuid);

                ALTER TABLE spaceos_modules.""ComponentSlots"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE spaceos_modules.""ComponentSlots"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""cs_tenant"" ON spaceos_modules.""ComponentSlots""
                    USING (""TenantId"" = current_setting('app.tenant_id')::uuid);

                ALTER TABLE spaceos_modules.""SlotConnections"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE spaceos_modules.""SlotConnections"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""sc_tenant"" ON spaceos_modules.""SlotConnections""
                    USING (""TenantId"" = current_setting('app.tenant_id')::uuid);

                ALTER TABLE spaceos_modules.""TemplateParameters"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE spaceos_modules.""TemplateParameters"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""tp_tenant"" ON spaceos_modules.""TemplateParameters""
                    USING (""TenantId"" = current_setting('app.tenant_id')::uuid);

                ALTER TABLE spaceos_modules.""GeometryAttachments"" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE spaceos_modules.""GeometryAttachments"" FORCE ROW LEVEL SECURITY;
                CREATE POLICY ""ga_tenant"" ON spaceos_modules.""GeometryAttachments""
                    USING (""TenantId"" = current_setting('app.tenant_id')::uuid);
            ", suppressTransaction: true);

            // DB-01: DAG cycle detection trigger
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION spaceos_modules.check_connection_dag()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF EXISTS (
                        WITH RECURSIVE path AS (
                            SELECT NEW.""ChildSlotId"" AS slot_id, 1 AS depth
                            UNION ALL
                            SELECT sc.""ChildSlotId"", p.depth + 1
                            FROM spaceos_modules.""SlotConnections"" sc
                            JOIN path p ON sc.""ParentSlotId"" = p.slot_id
                            WHERE p.depth < 50
                        )
                        SELECT 1 FROM path WHERE slot_id = NEW.""ParentSlotId""
                    ) THEN
                        RAISE EXCEPTION 'Cycle detected: connection %→% would create a loop',
                            NEW.""ParentSlotId"", NEW.""ChildSlotId"";
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER ""TR_SlotConnections_DagCheck""
                    BEFORE INSERT OR UPDATE ON spaceos_modules.""SlotConnections""
                    FOR EACH ROW EXECUTE FUNCTION spaceos_modules.check_connection_dag();
            ", suppressTransaction: true);

            // DB-03: Version immutability trigger
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION spaceos_modules.prevent_version_change()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF OLD.""Version"" <> NEW.""Version"" THEN
                        RAISE EXCEPTION 'ProductTemplate.Version is immutable — create a new version instead';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER ""TR_ProductTemplates_VersionImmutable""
                    BEFORE UPDATE ON spaceos_modules.""ProductTemplates""
                    FOR EACH ROW EXECUTE FUNCTION spaceos_modules.prevent_version_change();
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComponentSlots",
                schema: "spaceos_modules");

            migrationBuilder.DropTable(
                name: "GeometryAttachments",
                schema: "spaceos_modules");

            migrationBuilder.DropTable(
                name: "SlotConnections",
                schema: "spaceos_modules");

            migrationBuilder.DropTable(
                name: "TemplateParameters",
                schema: "spaceos_modules");

            migrationBuilder.DropTable(
                name: "ProductTemplates",
                schema: "spaceos_modules");
        }
    }
}
