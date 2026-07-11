using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialInventorySchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "spaceos_inventory");

        migrationBuilder.CreateTable(
            name: "MaterialCatalogs",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                StandardWidth = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                StandardHeight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                ThicknessMm = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                UnitCost = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                SupplierRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MaterialCatalogs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "PanelStocks",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                WidthMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                HeightMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                StockType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                LocationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PanelStocks", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Offcuts",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                WidthMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                HeightMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                OriginCuttingSheetId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Offcuts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "StockMovements",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MovementType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                MaterialCatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_StockMovements", x => x.Id));

        // Indexes
        migrationBuilder.CreateIndex(name: "IX_MaterialCatalogs_MaterialType", schema: "spaceos_inventory", table: "MaterialCatalogs", column: "MaterialType", unique: true);
        migrationBuilder.CreateIndex(name: "IX_PanelStocks_TenantId", schema: "spaceos_inventory", table: "PanelStocks", column: "TenantId");
        migrationBuilder.CreateIndex(name: "IX_PanelStocks_TenantId_MaterialCatalogId", schema: "spaceos_inventory", table: "PanelStocks", columns: new[] { "TenantId", "MaterialCatalogId" });
        migrationBuilder.CreateIndex(name: "IX_Offcuts_TenantId", schema: "spaceos_inventory", table: "Offcuts", column: "TenantId");
        migrationBuilder.CreateIndex(name: "IX_Offcuts_TenantId_Status", schema: "spaceos_inventory", table: "Offcuts", columns: new[] { "TenantId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_StockMovements_TenantId", schema: "spaceos_inventory", table: "StockMovements", column: "TenantId");
        migrationBuilder.CreateIndex(name: "IX_StockMovements_TenantId_OccurredAt", schema: "spaceos_inventory", table: "StockMovements", columns: new[] { "TenantId", "OccurredAt" });

        // Seed MaterialCatalog
        migrationBuilder.InsertData(schema: "spaceos_inventory", table: "MaterialCatalogs", columns: new[] { "Id", "MaterialType", "Description", "StandardWidth", "StandardHeight", "ThicknessMm", "UnitCost", "SupplierRef" },
            values: new object[,]
            {
                { new Guid("10000000-0000-0000-0000-000000000001"), "MDF 18mm", "MDF lap 18mm", 2800m, 2070m, 18m, 8500m, "MDF-18" },
                { new Guid("10000000-0000-0000-0000-000000000002"), "MDF 16mm", "MDF lap 16mm", 2800m, 2070m, 16m, 7800m, "MDF-16" },
                { new Guid("10000000-0000-0000-0000-000000000003"), "HDF 3mm", "HDF lap 3mm", 2800m, 2070m, 3m, 3200m, "HDF-3" },
                { new Guid("10000000-0000-0000-0000-000000000004"), "Forgácslap 18mm", "Forgácslap 18mm", 2800m, 2070m, 18m, 5500m, "PART-18" },
                { new Guid("10000000-0000-0000-0000-000000000005"), "ABS él 0.8mm", "ABS élzáró szalag 0.8mm", 50m, 25000m, 0.8m, 1200m, "ABS-08" }
            });

        // RLS: FORCE ROW LEVEL SECURITY on tenant tables
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_inventory.""PanelStocks"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""PanelStocks"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_inventory.""PanelStocks""
    USING (""TenantId"" = current_setting('app.current_tenant_id')::uuid);

ALTER TABLE spaceos_inventory.""Offcuts"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""Offcuts"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_inventory.""Offcuts""
    USING (""TenantId"" = current_setting('app.current_tenant_id')::uuid);

ALTER TABLE spaceos_inventory.""StockMovements"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""StockMovements"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_inventory.""StockMovements""
    USING (""TenantId"" = current_setting('app.current_tenant_id')::uuid);

REVOKE INSERT, UPDATE, DELETE ON spaceos_inventory.""MaterialCatalogs"" FROM spaceos_app;
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""PanelStocks"";
DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""Offcuts"";
DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""StockMovements"";
GRANT INSERT, UPDATE, DELETE ON spaceos_inventory.""MaterialCatalogs"" TO spaceos_app;
");
        migrationBuilder.DropTable(name: "StockMovements", schema: "spaceos_inventory");
        migrationBuilder.DropTable(name: "Offcuts", schema: "spaceos_inventory");
        migrationBuilder.DropTable(name: "PanelStocks", schema: "spaceos_inventory");
        migrationBuilder.DropTable(name: "MaterialCatalogs", schema: "spaceos_inventory");
    }
}
