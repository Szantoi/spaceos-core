using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Tenant_EmailHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailHash",
                table: "Tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_EmailHash",
                table: "Tenants",
                column: "EmailHash",
                unique: true,
                filter: "\"EmailHash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_EmailHash",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EmailHash",
                table: "Tenants");
        }
    }
}
