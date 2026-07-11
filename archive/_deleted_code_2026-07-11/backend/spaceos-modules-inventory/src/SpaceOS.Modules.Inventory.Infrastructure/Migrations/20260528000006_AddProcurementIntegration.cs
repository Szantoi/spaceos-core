using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProcurementIntegration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // MaterialCatalog: new fields for reorder alert payload
        migrationBuilder.AddColumn<int>(
            name: "ReorderPoint",
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            type: "integer",
            nullable: false,
            defaultValue: 5);

        migrationBuilder.AddColumn<int>(
            name: "SuggestedOrderQuantity",
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            type: "integer",
            nullable: false,
            defaultValue: 10);

        migrationBuilder.AddColumn<string>(
            name: "UnitOfMeasure",
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "pcs");

        migrationBuilder.AddColumn<Guid>(
            name: "PreferredSupplierId",
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            type: "uuid",
            nullable: true);

        // Procurement inbound inbox (idempotency)
        migrationBuilder.CreateTable(
            name: "InventoryInboundInboxes",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id            = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId      = table.Column<Guid>(type: "uuid", nullable: false),
                DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialCode  = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Quantity      = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                UnitOfMeasure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SupplierId    = table.Column<Guid>(type: "uuid", nullable: false),
                ReceivedAt    = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedAt   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_InventoryInboundInboxes", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_InventoryInboundInboxes_TenantId_DeliveryLineId",
            schema: "spaceos_inventory",
            table: "InventoryInboundInboxes",
            columns: new[] { "TenantId", "DeliveryLineId" },
            unique: true);

        // Reorder alert outbox
        migrationBuilder.CreateTable(
            name: "InventoryReorderOutboxes",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id             = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId       = table.Column<Guid>(type: "uuid", nullable: false),
                Payload        = table.Column<string>(type: "text", nullable: false),
                Status         = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                CreatedAt      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                NextAttemptAt  = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LeaseUntil     = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AttemptCount   = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastError      = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_InventoryReorderOutboxes", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_InventoryReorderOutboxes_Status_NextAttemptAt",
            schema: "spaceos_inventory",
            table: "InventoryReorderOutboxes",
            columns: new[] { "Status", "NextAttemptAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryReorderOutboxes_TenantId",
            schema: "spaceos_inventory",
            table: "InventoryReorderOutboxes",
            column: "TenantId");

        // Update seed data with new columns
        migrationBuilder.UpdateData(
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            keyColumn: "Id",
            keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
            columns: new[] { "ReorderPoint", "SuggestedOrderQuantity", "UnitOfMeasure", "PreferredSupplierId" },
            values: new object[] { 5, 10, "pcs", null });

        migrationBuilder.UpdateData(
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            keyColumn: "Id",
            keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
            columns: new[] { "ReorderPoint", "SuggestedOrderQuantity", "UnitOfMeasure", "PreferredSupplierId" },
            values: new object[] { 5, 10, "pcs", null });

        migrationBuilder.UpdateData(
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            keyColumn: "Id",
            keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
            columns: new[] { "ReorderPoint", "SuggestedOrderQuantity", "UnitOfMeasure", "PreferredSupplierId" },
            values: new object[] { 5, 10, "pcs", null });

        migrationBuilder.UpdateData(
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            keyColumn: "Id",
            keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
            columns: new[] { "ReorderPoint", "SuggestedOrderQuantity", "UnitOfMeasure", "PreferredSupplierId" },
            values: new object[] { 5, 10, "pcs", null });

        migrationBuilder.UpdateData(
            schema: "spaceos_inventory",
            table: "MaterialCatalogs",
            keyColumn: "Id",
            keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
            columns: new[] { "ReorderPoint", "SuggestedOrderQuantity", "UnitOfMeasure", "PreferredSupplierId" },
            values: new object[] { 5, 10, "pcs", null });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InventoryInboundInboxes", schema: "spaceos_inventory");
        migrationBuilder.DropTable(name: "InventoryReorderOutboxes", schema: "spaceos_inventory");

        migrationBuilder.DropColumn(name: "ReorderPoint",           schema: "spaceos_inventory", table: "MaterialCatalogs");
        migrationBuilder.DropColumn(name: "SuggestedOrderQuantity", schema: "spaceos_inventory", table: "MaterialCatalogs");
        migrationBuilder.DropColumn(name: "UnitOfMeasure",          schema: "spaceos_inventory", table: "MaterialCatalogs");
        migrationBuilder.DropColumn(name: "PreferredSupplierId",    schema: "spaceos_inventory", table: "MaterialCatalogs");
    }
}
