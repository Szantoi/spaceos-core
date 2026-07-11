using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGyartasilap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gyartasilaps",
                schema: "spaceos_joinery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoineryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PdfContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    StorageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LabelVariant = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gyartasilaps", x => x.Id);
                });

            // ── Indexes ──────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_Gyartasilaps_OrderId_Status",
                schema: "spaceos_joinery",
                table: "Gyartasilaps",
                columns: new[] { "JoineryOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Gyartasilaps_PlanId_CreatedAt",
                schema: "spaceos_joinery",
                table: "Gyartasilaps",
                columns: new[] { "CuttingPlanId", "CreatedAt" });

            // ── RLS (inherit from JoineryOrder via FK check) ──────────────────────

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""Gyartasilaps"" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""Gyartasilaps"" FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""Gyartasilaps""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""Gyartasilaps"";");
        }
    }
}
