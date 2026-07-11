using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AggregateId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchSequenceNumber",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "OutboxMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_BatchId_SeqNum",
                table: "OutboxMessages",
                columns: new[] { "BatchId", "BatchSequenceNumber" },
                unique: true,
                filter: "\"BatchId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status",
                table: "OutboxMessages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_BatchId_SeqNum",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Status",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "BatchSequenceNumber",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OutboxMessages");
        }
    }
}
