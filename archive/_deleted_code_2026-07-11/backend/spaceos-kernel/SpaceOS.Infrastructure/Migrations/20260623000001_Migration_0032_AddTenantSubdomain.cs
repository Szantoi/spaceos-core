using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0032 — Adds the <c>Subdomain</c> column to the <c>Tenants</c> table for Q3 Customer Portal subdomain-based tenant resolution.
/// </summary>
/// <remarks>
/// This migration supports Track A of Q3 Cutting Expansion, enabling public-facing B2C customer portal
/// where customers can submit quote requests via subdomain URLs (e.g., doorstar.joinerytech.hu).
/// The subdomain column is UNIQUE and indexed for fast lookups.
/// </remarks>
public partial class Migration_0032_AddTenantSubdomain : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Subdomain column (nullable initially to allow existing tenants)
        migrationBuilder.AddColumn<string>(
            name: "Subdomain",
            table: "Tenants",
            type: "text",
            nullable: true);

        // Create unique index on Subdomain
        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Subdomain",
            table: "Tenants",
            column: "Subdomain",
            unique: true);

        // Seed subdomain for existing Doorstar tenant (first customer)
        migrationBuilder.Sql("""
            UPDATE "Tenants"
            SET "Subdomain" = 'doorstar'
            WHERE "Name" = 'Doorstar Kft.';
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tenants_Subdomain",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "Subdomain",
            table: "Tenants");
    }
}
