using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SprintC_SchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalAuthToken",
                table: "SpaceLayers",
                newName: "ExternalAuthTokenRef");

            migrationBuilder.AddColumn<string>(
                name: "Handshake_ContractHash",
                table: "FlowEpics",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Handshake_InitiatorAnchorJson",
                table: "FlowEpics",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Handshake_ResponsibleAnchorJson",
                table: "FlowEpics",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Handshake_VisibilityScope",
                table: "FlowEpics",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofHash",
                table: "FlowEpics",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofUrl",
                table: "FlowEpics",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "AuditEvents",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                table: "AuditEvents",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "SHA256");

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "AuditEvents",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "GENESIS");

            migrationBuilder.AddColumn<string>(
                name: "SourceIp",
                table: "AuditEvents",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AggregateSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AggregateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TriggerEventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false),
                    SnapshotHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodeManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    PublicApiVersion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxGuestLod = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EpicId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NewState = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StateHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClientSignalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSyncedToKernel = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalUserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsErased = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateSnapshots_AggregateId",
                table: "AggregateSnapshots",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateSnapshots_TenantId",
                table: "AggregateSnapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeManifests_TenantId",
                table: "NodeManifests",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId",
                table: "OutboxMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_signals_epic",
                table: "SyncSignals",
                column: "EpicId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_signals_tenant_occurred",
                table: "SyncSignals",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncSignals_TenantId_ClientSignalId",
                table: "SyncSignals",
                columns: new[] { "TenantId", "ClientSignalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ExternalUserId_TenantId",
                table: "UserProfiles",
                columns: new[] { "ExternalUserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateSnapshots");

            migrationBuilder.DropTable(
                name: "NodeManifests");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "SyncSignals");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Handshake_ContractHash",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "Handshake_InitiatorAnchorJson",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "Handshake_ResponsibleAnchorJson",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "Handshake_VisibilityScope",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "ProofHash",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "ProofUrl",
                table: "FlowEpics");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "SourceIp",
                table: "AuditEvents");

            migrationBuilder.RenameColumn(
                name: "ExternalAuthTokenRef",
                table: "SpaceLayers",
                newName: "ExternalAuthToken");
        }
    }
}
