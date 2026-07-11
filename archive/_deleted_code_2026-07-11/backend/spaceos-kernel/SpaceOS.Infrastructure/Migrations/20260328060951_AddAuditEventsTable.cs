using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    StateHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_OccurredAt",
                table: "AuditEvents",
                columns: new[] { "TenantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
