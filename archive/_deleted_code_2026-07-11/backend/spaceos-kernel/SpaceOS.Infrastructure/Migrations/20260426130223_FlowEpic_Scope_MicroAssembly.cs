using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FlowEpic_Scope_MicroAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequiredSkillLevel",
                table: "FlowEpics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "FlowEpics",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FlowEpicRequiredResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    FlowEpicId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowEpicRequiredResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowEpicRequiredResources_FlowEpics_FlowEpicId",
                        column: x => x.FlowEpicId,
                        principalTable: "FlowEpics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlowEpicRequiredResources_FlowEpicId",
                table: "FlowEpicRequiredResources",
                column: "FlowEpicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlowEpicRequiredResources");

            migrationBuilder.DropColumn(
                name: "RequiredSkillLevel",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "FlowEpics");
        }
    }
}
