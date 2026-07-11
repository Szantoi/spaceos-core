using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowEpics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetFacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Handshake_GuestTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Handshake_DelegatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowEpics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceLayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsExternalNode = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalSourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExternalAuthToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IntentDataJson = table.Column<string>(type: "text", nullable: true),
                    LastStateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FacilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkStations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_TenantId",
                table: "Facilities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowEpics_TargetFacilityId",
                table: "FlowEpics",
                column: "TargetFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowEpics_TenantId",
                table: "FlowEpics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceLayers_FacilityId",
                table: "SpaceLayers",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceLayers_TenantId",
                table: "SpaceLayers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStations_FacilityId",
                table: "WorkStations",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStations_TenantId",
                table: "WorkStations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Facilities");

            migrationBuilder.DropTable(
                name: "FlowEpics");

            migrationBuilder.DropTable(
                name: "SpaceLayers");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "WorkStations");
        }
    }
}
