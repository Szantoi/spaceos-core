// SpaceOS.Infrastructure/Migrations/20260406090000_AddSourceBrandToAuditEvents.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <inheritdoc/>
public partial class AddSourceBrandToAuditEvents : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SourceBrand",
            table: "AuditEvents",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY "IX_AuditEvents_SourceBrand"
            ON "AuditEvents" ("SourceBrand")
            WHERE "SourceBrand" IS NOT NULL;
            """,
            suppressTransaction: true);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_AuditEvents_SourceBrand"";",
            suppressTransaction: true);

        migrationBuilder.DropColumn(name: "SourceBrand", table: "AuditEvents");
    }
}
