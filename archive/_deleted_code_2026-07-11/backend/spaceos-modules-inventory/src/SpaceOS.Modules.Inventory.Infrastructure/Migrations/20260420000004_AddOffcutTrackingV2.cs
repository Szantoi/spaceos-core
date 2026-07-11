using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOffcutTrackingV2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. Add new columns to Offcuts ────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "MaterialCode",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "ThicknessMm",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "numeric(10,2)",
            precision: 10, scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "VolumeM3",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "numeric(18,9)",
            precision: 18, scale: 9,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "WeightKg",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "numeric(10,3)",
            precision: 10, scale: 3,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<Guid>(
            name: "CuttingJobId",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UsedAt",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "UsedInJobId",
            schema: "spaceos_inventory",
            table: "Offcuts",
            type: "uuid",
            nullable: true);

        // ── 2. New indexes on Offcuts ─────────────────────────────────────────
        migrationBuilder.CreateIndex(
            name: "IX_Offcuts_Status_CreatedAt",
            schema: "spaceos_inventory",
            table: "Offcuts",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Offcuts_VolumeM3",
            schema: "spaceos_inventory",
            table: "Offcuts",
            column: "VolumeM3");

        // ── 3. RLS on Offcuts (was missing) ──────────────────────────────────
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_inventory.""Offcuts"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""Offcuts"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_inventory.""Offcuts""
    USING (""TenantId"" = current_setting('app.current_tenant_id', true)::uuid);
");

        // ── 4. Create OffcutReservations table ───────────────────────────────
        migrationBuilder.CreateTable(
            name: "OffcutReservations",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uuid", nullable: false),
                OffcutId  = table.Column<Guid>(type: "uuid", nullable: false),
                JobId     = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId  = table.Column<Guid>(type: "uuid", nullable: false),
                Status    = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OffcutReservations", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_OffcutReservations_OffcutId",
            schema: "spaceos_inventory",
            table: "OffcutReservations",
            column: "OffcutId");

        migrationBuilder.CreateIndex(
            name: "IX_OffcutReservations_TenantId_Status",
            schema: "spaceos_inventory",
            table: "OffcutReservations",
            columns: new[] { "TenantId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_OffcutReservations_ExpiresAt",
            schema: "spaceos_inventory",
            table: "OffcutReservations",
            column: "ExpiresAt");

        // ── 5. RLS on OffcutReservations ─────────────────────────────────────
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_inventory.""OffcutReservations"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""OffcutReservations"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_inventory.""OffcutReservations""
    USING (""TenantId"" = current_setting('app.current_tenant_id', true)::uuid);
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""OffcutReservations"";
ALTER TABLE spaceos_inventory.""OffcutReservations"" DISABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""Offcuts"";
ALTER TABLE spaceos_inventory.""Offcuts"" DISABLE ROW LEVEL SECURITY;
");

        migrationBuilder.DropTable(name: "OffcutReservations", schema: "spaceos_inventory");

        migrationBuilder.DropIndex(name: "IX_Offcuts_Status_CreatedAt", schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropIndex(name: "IX_Offcuts_VolumeM3", schema: "spaceos_inventory", table: "Offcuts");

        migrationBuilder.DropColumn(name: "MaterialCode",  schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "ThicknessMm",   schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "VolumeM3",      schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "WeightKg",      schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "CuttingJobId",  schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "UsedAt",        schema: "spaceos_inventory", table: "Offcuts");
        migrationBuilder.DropColumn(name: "UsedInJobId",   schema: "spaceos_inventory", table: "Offcuts");
    }
}
