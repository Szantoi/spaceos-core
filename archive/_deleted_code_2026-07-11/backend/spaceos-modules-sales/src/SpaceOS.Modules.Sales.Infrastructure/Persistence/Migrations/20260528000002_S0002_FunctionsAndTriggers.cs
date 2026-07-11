using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class S0002_FunctionsAndTriggers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- ─── fn_next_quote_number ────────────────────────────────────────────────────
-- Advisory-lock-based, race-free sequence per tenant/year (DB-S-03).
-- Returns format: Q-YYYY-NNNNN
CREATE OR REPLACE FUNCTION spaceos_sales.fn_next_quote_number(
    p_tenant_id uuid,
    p_year      integer
) RETURNS text
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_seq integer;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext(p_tenant_id::text));

    INSERT INTO spaceos_sales.quote_number_counters (tenant_id, year, next_seq)
    VALUES (p_tenant_id, p_year, 1)
    ON CONFLICT (tenant_id, year) DO UPDATE
        SET next_seq = quote_number_counters.next_seq + 1
    RETURNING next_seq - 1 INTO v_seq;

    RETURN 'Q-' || p_year::text || '-' || lpad(v_seq::text, 5, '0');
END;
$$;

GRANT EXECUTE ON FUNCTION spaceos_sales.fn_next_quote_number(uuid, integer) TO spaceos_sales_app;

-- ─── fn_prevent_quote_content_update (DB-S-01) ───────────────────────────────
-- Blocks updates to content fields (lines, currency, customer) after a quote leaves Draft.
CREATE OR REPLACE FUNCTION spaceos_sales.fn_prevent_quote_content_update()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.""Status"" <> 'Draft' THEN
        IF NEW.""CustomerId"" <> OLD.""CustomerId""
            OR NEW.""Currency"" <> OLD.""Currency""
            OR NEW.""QuoteNumber"" <> OLD.""QuoteNumber"" THEN
            RAISE EXCEPTION 'Quote content is immutable after leaving Draft status.';
        END IF;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_prevent_quote_content_update
    BEFORE UPDATE ON spaceos_sales.""Quotes""
    FOR EACH ROW EXECUTE FUNCTION spaceos_sales.fn_prevent_quote_content_update();

-- ─── fn_prevent_quote_line_changes_after_sent (DB-S-02) ──────────────────────
-- Blocks line inserts/updates/deletes once the parent quote is in Sent or later.
CREATE OR REPLACE FUNCTION spaceos_sales.fn_prevent_quote_line_changes_after_sent()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_status varchar(20);
BEGIN
    SELECT ""Status"" INTO v_status
    FROM spaceos_sales.""Quotes""
    WHERE ""Id"" = COALESCE(NEW.""QuoteId"", OLD.""QuoteId"");

    IF v_status <> 'Draft' THEN
        RAISE EXCEPTION 'Cannot modify lines on a Quote that has left Draft status.';
    END IF;
    RETURN COALESCE(NEW, OLD);
END;
$$;

CREATE TRIGGER trg_prevent_quote_line_changes_after_sent
    BEFORE INSERT OR UPDATE OR DELETE ON spaceos_sales.""QuoteLines""
    FOR EACH ROW EXECUTE FUNCTION spaceos_sales.fn_prevent_quote_line_changes_after_sent();
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_prevent_quote_line_changes_after_sent ON spaceos_sales.""QuoteLines"";
DROP FUNCTION IF EXISTS spaceos_sales.fn_prevent_quote_line_changes_after_sent();
DROP TRIGGER IF EXISTS trg_prevent_quote_content_update ON spaceos_sales.""Quotes"";
DROP FUNCTION IF EXISTS spaceos_sales.fn_prevent_quote_content_update();
DROP FUNCTION IF EXISTS spaceos_sales.fn_next_quote_number(uuid, integer);
");
    }
}
