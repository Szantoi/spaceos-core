using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class J003_SalesIntegrationReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. New nullable columns on DoorOrders ─────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedTenantId",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceQuoteId",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceContentHash",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedFromSalesAt",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalNet",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVat",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalGross",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                type: "decimal(18,4)",
                nullable: true);

            // ── 2. Partial unique index for idempotency ───────────────────────

            migrationBuilder.CreateIndex(
                name: "UX_DoorOrders_TenantId_SourceQuoteId",
                schema: "spaceos_joinery",
                table: "DoorOrders",
                columns: new[] { "TenantId", "SourceQuoteId" },
                unique: true,
                filter: "\"SourceQuoteId\" IS NOT NULL");

            // ── 3. Update Status CHECK constraint to include ConfirmedFromSales ─

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrders""
    DROP CONSTRAINT IF EXISTS ""CK_DoorOrders_Status"";");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrders""
    ADD CONSTRAINT ""CK_DoorOrders_Status""
    CHECK (""Status"" IN (
        'Draft', 'ConfirmedFromSales', 'Submitted', 'Calculating',
        'Calculated', 'CalculationFailed', 'InProduction', 'Completed', 'Cancelled'
    ));");

            // ── 4. DoorOrderConvertedLines table ──────────────────────────────

            migrationBuilder.CreateTable(
                name: "DoorOrderConvertedLines",
                schema: "spaceos_joinery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoorOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPriceNet = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(6,4)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(6,4)", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorOrderConvertedLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoorOrderConvertedLines_DoorOrders_DoorOrderId",
                        column: x => x.DoorOrderId,
                        principalSchema: "spaceos_joinery",
                        principalTable: "DoorOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoorOrderConvertedLines_OrderId",
                schema: "spaceos_joinery",
                table: "DoorOrderConvertedLines",
                column: "DoorOrderId");

            // ── 5. RLS on DoorOrderConvertedLines ─────────────────────────────

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrderConvertedLines"" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrderConvertedLines"" FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation_converted_lines ON spaceos_joinery.""DoorOrderConvertedLines""
    USING (
        ""DoorOrderId"" IN (
            SELECT ""Id"" FROM spaceos_joinery.""DoorOrders""
            WHERE ""TenantId"" = current_setting('app.tenant_id', true)::uuid
        )
    );");

            // ── 6. GRANT to spaceos_joinery_app ──────────────────────────────

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'spaceos_joinery_app') THEN
        GRANT SELECT, INSERT, UPDATE, DELETE
            ON spaceos_joinery.""DoorOrderConvertedLines""
            TO spaceos_joinery_app;
    END IF;
END
$$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS spaceos_joinery.""DoorOrderConvertedLines"";");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""DoorOrders""
    DROP CONSTRAINT IF EXISTS ""CK_DoorOrders_Status"";");

            migrationBuilder.DropIndex(
                name: "UX_DoorOrders_TenantId_SourceQuoteId",
                schema: "spaceos_joinery",
                table: "DoorOrders");

            migrationBuilder.DropColumn(name: "CustomerId", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "LinkedTenantId", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "SourceQuoteId", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "SourceContentHash", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "ConfirmedFromSalesAt", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "Currency", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "TotalNet", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "TotalVat", schema: "spaceos_joinery", table: "DoorOrders");
            migrationBuilder.DropColumn(name: "TotalGross", schema: "spaceos_joinery", table: "DoorOrders");
        }
    }
}
