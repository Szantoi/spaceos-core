using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SpaceOS.Infrastructure.Migrations;

/// <summary>
/// Migration 0031 — Creates the <c>InternalAccessAuditLog</c> table for SEC-S-09 internal actor directory audit trail.
/// </summary>
/// <remarks>
/// Append-only table: <c>spaceos_app</c> is granted SELECT + INSERT only.
/// UPDATE and DELETE are explicitly revoked so no row can ever be modified or removed
/// through the application role, satisfying the WORM requirement for the internal lookup audit.
/// </remarks>
public partial class Migration_0031_AddInternalAccessAuditLog : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InternalAccessAuditLog",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                RequesterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                TargetTenantId    = table.Column<Guid>(type: "uuid", nullable: false),
                Result            = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                OccurredAt        = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InternalAccessAuditLog", x => x.Id);
                table.CheckConstraint("CK_InternalAudit_Result",
                    "\"Result\" IN ('Found', 'NotFound')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_InternalAudit_Requester_OccurredAt",
            table: "InternalAccessAuditLog",
            columns: new[] { "RequesterTenantId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InternalAudit_Target_OccurredAt",
            table: "InternalAccessAuditLog",
            columns: new[] { "TargetTenantId", "OccurredAt" });

        migrationBuilder.Sql("""
            REVOKE UPDATE, DELETE ON "InternalAccessAuditLog" FROM spaceos_app;
            GRANT  SELECT, INSERT  ON "InternalAccessAuditLog" TO   spaceos_app;
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InternalAccessAuditLog");
    }
}
