using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOffcutBatch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OffcutBatches",
            schema: "spaceos_inventory",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId  = table.Column<Guid>(type: "uuid", nullable: false),
                SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SourceId  = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OffcutBatches", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OffcutBatches_TenantId_SourceType_SourceId",
            schema: "spaceos_inventory",
            table: "OffcutBatches",
            columns: new[] { "TenantId", "SourceType", "SourceId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OffcutBatches",
            schema: "spaceos_inventory");
    }
}
