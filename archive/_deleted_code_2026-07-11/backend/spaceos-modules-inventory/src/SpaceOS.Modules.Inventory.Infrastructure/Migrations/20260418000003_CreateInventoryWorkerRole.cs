using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Inventory.Infrastructure.Migrations;

/// <inheritdoc />
public partial class CreateInventoryWorkerRole : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'spaceos_inventory_worker') THEN
                    CREATE ROLE spaceos_inventory_worker WITH
                        LOGIN BYPASSRLS NOCREATEDB NOCREATEROLE NOINHERIT PASSWORD NULL;
                END IF;
            END $$;

            GRANT USAGE ON SCHEMA spaceos_inventory TO spaceos_inventory_worker;
            GRANT SELECT, UPDATE ON spaceos_inventory.reservations TO spaceos_inventory_worker;
            GRANT SELECT ON spaceos_inventory.reservation_items TO spaceos_inventory_worker;
            REVOKE ALL ON spaceos_inventory.panel_stocks FROM spaceos_inventory_worker;
            REVOKE ALL ON spaceos_inventory.material_catalog FROM spaceos_inventory_worker;
            REVOKE ALL ON spaceos_inventory.stock_movements FROM spaceos_inventory_worker;

            COMMENT ON ROLE spaceos_inventory_worker IS
                'ADR-024: Background worker role. BYPASSRLS for cross-tenant cleanup. Narrow grants only.';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "REVOKE ALL ON SCHEMA spaceos_inventory FROM spaceos_inventory_worker;");
    }
}
