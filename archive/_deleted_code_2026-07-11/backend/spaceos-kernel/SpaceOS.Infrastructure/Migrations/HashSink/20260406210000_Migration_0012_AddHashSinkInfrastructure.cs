// SpaceOS.Infrastructure/Migrations/HashSink/20260406210000_Migration_0012_AddHashSinkInfrastructure.cs

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations.HashSink;

/// <summary>
/// Migration 0012 — Creates the <c>hash_chain_records</c> table in the
/// <c>spaceos_audit_sink</c> database (BE-P15-08, BE-P15-09).
/// </summary>
/// <remarks>
/// Apply with the explicit context and connection flags:
/// <code>
///   dotnet ef database update
///     --context HashSinkDbContext
///     --connection "Host=127.0.0.1;Database=spaceos_audit_sink;Username=spaceos_sink_writer;Password=..."
///     --project SpaceOS.Infrastructure
///     --startup-project SpaceOS.Kernel.Api
/// </code>
/// The <c>spaceos_sink_writer</c> role requires only INSERT on <c>hash_chain_records</c>;
/// migration must be applied by a superuser or dedicated migration role with CREATE TABLE rights.
/// </remarks>
public partial class Migration_0012_AddHashSinkInfrastructure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "hash_chain_records",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenantId   = table.Column<Guid>(type: "uuid", nullable: false),
                EventId    = table.Column<Guid>(type: "uuid", nullable: false),
                StateHash  = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                InsertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone",
                    nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_hash_chain_records", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_hash_chain_records_EventId",
            table: "hash_chain_records",
            column: "EventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_hash_chain_records_TenantId",
            table: "hash_chain_records",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_hash_chain_records_OccurredAt",
            table: "hash_chain_records",
            column: "OccurredAt");

        // Grant INSERT-only access to the sink writer role.
        // The spaceos_sink_verifier role (SELECT) is granted separately in init-roles.sql.
        migrationBuilder.Sql("""
            DO $$ BEGIN
              IF EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sink_writer') THEN
                GRANT INSERT ON hash_chain_records TO spaceos_sink_writer;
              END IF;
            END $$;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "hash_chain_records");
    }
}
