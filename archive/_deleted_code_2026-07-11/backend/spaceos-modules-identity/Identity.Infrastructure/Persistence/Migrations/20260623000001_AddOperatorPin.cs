// Identity.Infrastructure/Persistence/Migrations/20260623000001_AddOperatorPin.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddOperatorPin : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "operator_pin",
            schema: "identity",
            table: "spaceos_users",
            type: "character varying(4)",
            maxLength: 4,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "idx_spaceos_users_operator_pin",
            schema: "identity",
            table: "spaceos_users",
            column: "operator_pin",
            unique: false,
            filter: "operator_pin IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "idx_spaceos_users_operator_pin",
            schema: "identity",
            table: "spaceos_users");

        migrationBuilder.DropColumn(
            name: "operator_pin",
            schema: "identity",
            table: "spaceos_users");
    }
}
