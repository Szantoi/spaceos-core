// SpaceOS.Infrastructure/Migrations/20260407190000_Migration_0024_TenantsBrandSkinId.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0024 — Adds the <c>BrandSkinId</c> column to the <c>Tenants</c> table.
/// <para>
/// The column is nullable (<c>character varying(64) NULL</c>); a <c>null</c> value means
/// the tenant uses the default brand skin ("joinerytech"). The application layer applies
/// this fallback at token-issuance time (MSG-K028).
/// </para>
/// </summary>
public partial class Migration_0024_TenantsBrandSkinId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BrandSkinId",
            table: "Tenants",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BrandSkinId",
            table: "Tenants");
    }
}
