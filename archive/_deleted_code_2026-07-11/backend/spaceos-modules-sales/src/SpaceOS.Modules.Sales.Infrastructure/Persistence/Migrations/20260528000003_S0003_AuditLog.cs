using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class S0003_AuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- ─── Audit log (SEC-S-08) ────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.sales_audit_log (
    ""Id""            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ""TenantId""      uuid         NOT NULL,
    ""ActorSub""      varchar(200) NOT NULL,
    ""AggregateType"" varchar(50)  NOT NULL,
    ""AggregateId""   uuid         NOT NULL,
    ""Operation""     varchar(50)  NOT NULL,
    ""PayloadHash""   char(64)     NOT NULL,
    ""OccurredAt""    timestamptz  NOT NULL DEFAULT now()
);

CREATE INDEX ""IX_SalesAudit_Tenant_Aggregate""
    ON spaceos_sales.sales_audit_log(""TenantId"", ""AggregateType"", ""AggregateId"");

-- App role: INSERT + SELECT only; no UPDATE/DELETE (immutable audit)
REVOKE ALL ON spaceos_sales.sales_audit_log FROM PUBLIC;
GRANT SELECT, INSERT ON spaceos_sales.sales_audit_log TO spaceos_sales_app;
GRANT SELECT, INSERT ON spaceos_sales.sales_audit_log TO spaceos_sales_worker;

ALTER TABLE spaceos_sales.sales_audit_log ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_sales.sales_audit_log FORCE ROW LEVEL SECURITY;

CREATE POLICY sales_audit_tenant ON spaceos_sales.sales_audit_log
    USING (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid))
    WITH CHECK (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid));
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TABLE IF EXISTS spaceos_sales.sales_audit_log CASCADE;
");
    }
}
