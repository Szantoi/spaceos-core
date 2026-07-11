using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGyartasilapBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GyartasilapBatches",
                schema: "spaceos_joinery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GyartasilapIds = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ZipStoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GyartasilapBatches", x => x.Id);
                });

            // ── Indexes ──────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_GyartasilapBatches_OrderId_Status",
                schema: "spaceos_joinery",
                table: "GyartasilapBatches",
                columns: new[] { "OrderId", "Status" });

            // ── RLS ──────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""GyartasilapBatches"" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""GyartasilapBatches"" FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""GyartasilapBatches""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""GyartasilapBatches"";");
        }
    }
}
