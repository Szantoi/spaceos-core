using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Joinery.Infrastructure.Migrations;

/// <inheritdoc />
public partial class J005_WorkOrderOperations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // WorkOrderOperations table - Assembly operations for production planning
        migrationBuilder.Sql(@"
CREATE TABLE spaceos_joinery.""WorkOrderOperations"" (
    ""Id""                  uuid            NOT NULL,
    ""TenantId""            uuid            NOT NULL,
    ""WorkOrderId""         uuid            NOT NULL,
    ""Sequence""            integer         NOT NULL,
    ""Description""         varchar(500)    NOT NULL,
    ""EstimatedDuration""   interval        NOT NULL,
    ""OperationType""       varchar(100)    NOT NULL,
    ""LastModified""        timestamptz     NOT NULL DEFAULT now(),
    ""CreatedAt""           timestamptz     NOT NULL DEFAULT now(),
    CONSTRAINT ""PK_WorkOrderOperations"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_WorkOrderOperations_WorkOrders"" FOREIGN KEY (""WorkOrderId"")
        REFERENCES spaceos_joinery.""WorkOrders"" (""Id"") ON DELETE CASCADE
);");

        // Indexes
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrderOperations_TenantId"" ON spaceos_joinery.""WorkOrderOperations"" (""TenantId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrderOperations_WorkOrderId"" ON spaceos_joinery.""WorkOrderOperations"" (""WorkOrderId"");");
        migrationBuilder.Sql(@"CREATE INDEX ""IX_WorkOrderOperations_WorkOrderId_Sequence"" ON spaceos_joinery.""WorkOrderOperations"" (""WorkOrderId"", ""Sequence"");");

        // RLS for WorkOrderOperations
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""WorkOrderOperations"" ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"ALTER TABLE spaceos_joinery.""WorkOrderOperations"" FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql(@"
CREATE POLICY tenant_isolation ON spaceos_joinery.""WorkOrderOperations""
    USING (""TenantId"" = current_setting('app.tenant_id', true)::uuid);");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS spaceos_joinery.""WorkOrderOperations"" CASCADE;");
    }
}
