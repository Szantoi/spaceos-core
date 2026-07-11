using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnyaglista : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anyaglistak",
                schema: "spaceos_joinery",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PdfContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    StorageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anyaglistak", x => x.Id);
                });

            // ── Index ────────────────────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_Anyaglistak_OrderId",
                schema: "spaceos_joinery",
                table: "Anyaglistak",
                column: "OrderId");

            // ── RLS ──────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""Anyaglistak"" ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
ALTER TABLE spaceos_joinery.""Anyaglistak"" FORCE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""Anyaglistak""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""Anyaglistak"";");
        }
    }
}
