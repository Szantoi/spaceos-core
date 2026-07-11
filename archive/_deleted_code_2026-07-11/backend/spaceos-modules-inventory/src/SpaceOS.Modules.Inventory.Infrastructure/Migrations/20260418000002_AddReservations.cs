using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <summary>
/// Migration 0030: Add Reservations + ReservationItems tables with RLS, partial unique index,
/// tenant isolation trigger, and v_stock_availability view.
/// Implemented as pure SQL to support PostgreSQL-specific DDL (xmin, jsonb, partial index, RLS).
/// </summary>
public partial class AddReservations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- ============================================================
-- TABLE: Reservations
-- ============================================================
CREATE TABLE spaceos_inventory.""Reservations"" (
    ""Id""                 uuid            NOT NULL DEFAULT gen_random_uuid(),
    ""TenantId""           uuid            NOT NULL,
    ""CorrelationId""      uuid            NOT NULL,
    ""ConsumerModule""     varchar(50)     NOT NULL,
    ""ConsumerContextJson"" jsonb           NULL,
    ""CreatedByUserId""    uuid            NULL,
    ""CreatedAt""          timestamptz     NOT NULL,
    ""ExpiresAt""          timestamptz     NOT NULL,
    ""Status""             integer         NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_Reservations"" PRIMARY KEY (""Id"")
);

-- ============================================================
-- TABLE: ReservationItems
-- ============================================================
CREATE TABLE spaceos_inventory.""ReservationItems"" (
    ""Id""                 uuid            NOT NULL DEFAULT gen_random_uuid(),
    ""ReservationId""      uuid            NOT NULL,
    ""TenantId""           uuid            NOT NULL,
    ""StockItemId""        uuid            NOT NULL,
    ""MaterialCode""       varchar(20)     NOT NULL,
    ""QuantityReserved""   numeric(18,4)   NOT NULL,
    ""QuantityConsumed""   numeric(18,4)   NOT NULL DEFAULT 0,
    CONSTRAINT ""PK_ReservationItems"" PRIMARY KEY (""Id""),
    CONSTRAINT ""FK_ReservationItems_Reservations"" FOREIGN KEY (""ReservationId"")
        REFERENCES spaceos_inventory.""Reservations""(""Id"") ON DELETE CASCADE
);

-- ============================================================
-- INDEXES: Reservations
-- ============================================================
CREATE INDEX ""IX_Reservations_TenantId_CorrelationId""
    ON spaceos_inventory.""Reservations""(""TenantId"", ""CorrelationId"");

-- I-04: partial unique index — only one Active reservation per (tenant, correlationId)
CREATE UNIQUE INDEX ""ux_reservations_tenant_correlation_active""
    ON spaceos_inventory.""Reservations""(""TenantId"", ""CorrelationId"")
    WHERE ""Status"" = 0;

-- ============================================================
-- INDEXES: ReservationItems
-- ============================================================
CREATE INDEX ""IX_ReservationItems_TenantId""
    ON spaceos_inventory.""ReservationItems""(""TenantId"");

CREATE INDEX ""IX_ReservationItems_StockItemId""
    ON spaceos_inventory.""ReservationItems""(""StockItemId"");

-- ============================================================
-- TENANT ISOLATION TRIGGER FUNCTION
-- Ensures every inserted/updated row carries the correct TenantId
-- ============================================================
CREATE OR REPLACE FUNCTION spaceos_inventory.fn_enforce_reservation_tenant()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF NEW.""TenantId"" IS NULL OR
       NEW.""TenantId"" <> current_setting('app.current_tenant_id', true)::uuid
    THEN
        RAISE EXCEPTION 'Tenant mismatch on Reservations: expected % got %',
            current_setting('app.current_tenant_id', true), NEW.""TenantId"";
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_reservations_tenant_check
    BEFORE INSERT OR UPDATE ON spaceos_inventory.""Reservations""
    FOR EACH ROW EXECUTE FUNCTION spaceos_inventory.fn_enforce_reservation_tenant();

-- ============================================================
-- ROW LEVEL SECURITY: Reservations
-- ============================================================
ALTER TABLE spaceos_inventory.""Reservations"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""Reservations"" FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON spaceos_inventory.""Reservations""
    USING (""TenantId"" = current_setting('app.current_tenant_id')::uuid);

-- ============================================================
-- ROW LEVEL SECURITY: ReservationItems
-- ============================================================
ALTER TABLE spaceos_inventory.""ReservationItems"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_inventory.""ReservationItems"" FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON spaceos_inventory.""ReservationItems""
    USING (""TenantId"" = current_setting('app.current_tenant_id')::uuid);

-- ============================================================
-- VIEW: v_stock_availability  (security_invoker = true)
-- Shows net available quantity: stocked - actively reserved
-- ============================================================
CREATE OR REPLACE VIEW spaceos_inventory.v_stock_availability
WITH (security_invoker = true) AS
SELECT
    ps.""Id""                AS ""StockItemId"",
    ps.""TenantId"",
    ps.""MaterialCatalogId"",
    ps.""Quantity""          AS ""TotalQuantity"",
    COALESCE(SUM(ri.""QuantityReserved"" - ri.""QuantityConsumed""), 0) AS ""ReservedQuantity"",
    ps.""Quantity"" - COALESCE(SUM(ri.""QuantityReserved"" - ri.""QuantityConsumed""), 0) AS ""AvailableQuantity""
FROM spaceos_inventory.""PanelStocks"" ps
LEFT JOIN spaceos_inventory.""ReservationItems"" ri
    ON ri.""StockItemId"" = ps.""Id""
    AND ri.""TenantId""   = ps.""TenantId""
LEFT JOIN spaceos_inventory.""Reservations"" r
    ON r.""Id""       = ri.""ReservationId""
    AND r.""Status""  = 0  -- Active only
GROUP BY ps.""Id"", ps.""TenantId"", ps.""MaterialCatalogId"", ps.""Quantity"";
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- Drop in reverse order

DROP VIEW IF EXISTS spaceos_inventory.v_stock_availability;

DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""ReservationItems"";
ALTER TABLE spaceos_inventory.""ReservationItems"" DISABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS tenant_isolation ON spaceos_inventory.""Reservations"";
ALTER TABLE spaceos_inventory.""Reservations"" DISABLE ROW LEVEL SECURITY;

DROP TRIGGER IF EXISTS trg_reservations_tenant_check ON spaceos_inventory.""Reservations"";
DROP FUNCTION IF EXISTS spaceos_inventory.fn_enforce_reservation_tenant();

DROP TABLE IF EXISTS spaceos_inventory.""ReservationItems"";
DROP TABLE IF EXISTS spaceos_inventory.""Reservations"";
");
    }
}
