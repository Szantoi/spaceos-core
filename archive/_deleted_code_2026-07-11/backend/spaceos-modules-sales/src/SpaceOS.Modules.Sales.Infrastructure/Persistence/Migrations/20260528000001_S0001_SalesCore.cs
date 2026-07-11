using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Sales.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class S0001_SalesCore : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- ─── Schema & roles ──────────────────────────────────────────────────────────
CREATE SCHEMA IF NOT EXISTS spaceos_sales;

DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sales_app') THEN
    CREATE ROLE spaceos_sales_app LOGIN NOINHERIT;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sales_worker') THEN
    CREATE ROLE spaceos_sales_worker LOGIN NOINHERIT;
  END IF;
END $$;

GRANT USAGE ON SCHEMA spaceos_sales TO spaceos_sales_app, spaceos_sales_worker;

-- ─── Customers ───────────────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.""Customers"" (
    ""Id""                  uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    ""TenantId""            uuid         NOT NULL,
    ""Type""                varchar(20)  NOT NULL,
    ""DisplayName""         varchar(200) NOT NULL,
    ""CompanyTaxNumber""    varchar(50),
    ""ContactName""         varchar(200) NOT NULL,
    ""ContactEmail""        varchar(320),
    ""ContactPhone""        varchar(50),
    ""BillingAddressJson""  jsonb,
    ""ShippingAddressJson"" jsonb,
    ""Status""              varchar(20)  NOT NULL DEFAULT 'Lead',
    ""LinkedTenantId""      uuid,
    ""LinkedAt""            timestamptz,
    ""LinkStatus""          varchar(20)  NOT NULL DEFAULT 'None',
    ""LinkVerifiedAt""      timestamptz,
    ""Notes""               varchar(2000),
    ""IsArchived""          boolean      NOT NULL DEFAULT false,
    ""CreatedAt""           timestamptz  NOT NULL DEFAULT now(),
    ""CreatedBy""           varchar(200) NOT NULL,
    ""UpdatedAt""           timestamptz
);

CREATE INDEX ""IX_Customers_TenantId"" ON spaceos_sales.""Customers""(""TenantId"");

GRANT SELECT, INSERT, UPDATE ON spaceos_sales.""Customers"" TO spaceos_sales_app;

ALTER TABLE spaceos_sales.""Customers"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_sales.""Customers"" FORCE ROW LEVEL SECURITY;

CREATE POLICY sales_customers_tenant ON spaceos_sales.""Customers""
    USING (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid))
    WITH CHECK (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid));

-- ─── Quotes ──────────────────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.""Quotes"" (
    ""Id""                        uuid            NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    ""TenantId""                  uuid            NOT NULL,
    ""CustomerId""                uuid            NOT NULL,
    ""QuoteNumber""               varchar(15)     NOT NULL,
    ""Status""                    varchar(20)     NOT NULL DEFAULT 'Draft',
    ""Currency""                  char(3)         NOT NULL DEFAULT 'HUF',
    ""ValidUntil""                timestamptz,
    ""Notes""                     varchar(2000),
    ""TotalNetAmount""            decimal(14,2)   NOT NULL DEFAULT 0,
    ""TotalVatAmount""            decimal(14,2)   NOT NULL DEFAULT 0,
    ""TotalGrossAmount""          decimal(14,2)   NOT NULL DEFAULT 0,
    ""ContentHash""               char(64),
    ""RejectionReason""           varchar(500),
    ""ConversionFailureReason""   varchar(1000),
    ""ConversionRequestedAt""     timestamptz,
    ""ConvertedAt""               timestamptz,
    ""ConvertedOrderId""          uuid,
    ""SentAt""                    timestamptz,
    ""AcceptedAt""                timestamptz,
    ""RejectedAt""                timestamptz,
    ""IsArchived""                boolean         NOT NULL DEFAULT false,
    ""CreatedAt""                 timestamptz     NOT NULL DEFAULT now(),
    ""CreatedBy""                 varchar(200)    NOT NULL
);

CREATE INDEX ""IX_Quotes_Tenant_Status"" ON spaceos_sales.""Quotes""(""TenantId"", ""Status"");

GRANT SELECT, INSERT, UPDATE ON spaceos_sales.""Quotes"" TO spaceos_sales_app;
GRANT SELECT, UPDATE ON spaceos_sales.""Quotes"" TO spaceos_sales_worker;

ALTER TABLE spaceos_sales.""Quotes"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_sales.""Quotes"" FORCE ROW LEVEL SECURITY;

CREATE POLICY sales_quotes_tenant ON spaceos_sales.""Quotes""
    USING (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid))
    WITH CHECK (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid));

-- ─── QuoteLines ───────────────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.""QuoteLines"" (
    ""Id""               uuid           NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    ""QuoteId""          uuid           NOT NULL REFERENCES spaceos_sales.""Quotes""(""Id"") ON DELETE CASCADE,
    ""TenantId""         uuid           NOT NULL,
    ""LineType""         varchar(20)    NOT NULL,
    ""SourceTemplateId"" uuid,
    ""Description""      varchar(500)   NOT NULL,
    ""Quantity""         decimal(12,3)  NOT NULL,
    ""UnitPriceAmount""  decimal(14,2)  NOT NULL,
    ""UnitPriceCurrency"" char(3)       NOT NULL DEFAULT 'HUF',
    ""VatRate""          decimal(5,4)   NOT NULL,
    ""DiscountPercent""  decimal(5,4),
    ""SortOrder""        integer        NOT NULL,
    ""LineNetAmount""    decimal(14,2)  NOT NULL,
    ""LineVatAmount""    decimal(14,2)  NOT NULL,
    ""LineGrossAmount""  decimal(14,2)  NOT NULL
);

CREATE INDEX ""IX_QuoteLines_QuoteId"" ON spaceos_sales.""QuoteLines""(""QuoteId"");

GRANT SELECT, INSERT, UPDATE, DELETE ON spaceos_sales.""QuoteLines"" TO spaceos_sales_app;

ALTER TABLE spaceos_sales.""QuoteLines"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_sales.""QuoteLines"" FORCE ROW LEVEL SECURITY;

CREATE POLICY sales_quote_lines_tenant ON spaceos_sales.""QuoteLines""
    USING (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid))
    WITH CHECK (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid));

-- ─── Outbox ───────────────────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.sales_outbox (
    ""Id""              uuid          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    ""TenantId""        uuid          NOT NULL,
    ""AggregateId""     uuid          NOT NULL,
    ""Operation""       varchar(50)   NOT NULL,
    ""PayloadJson""     jsonb         NOT NULL,
    ""IdempotencyKey""  varchar(64)   NOT NULL,
    ""Status""          varchar(20)   NOT NULL DEFAULT 'Pending',
    ""AttemptCount""    integer       NOT NULL DEFAULT 0,
    ""NextAttemptAt""   timestamptz   NOT NULL DEFAULT now(),
    ""CreatedAt""       timestamptz   NOT NULL DEFAULT now(),
    ""ProcessedAt""     timestamptz,
    ""LastError""       varchar(2000)
);

CREATE INDEX ""IX_SalesOutbox_Pending"" ON spaceos_sales.sales_outbox(""NextAttemptAt"")
    WHERE ""Status"" IN ('Pending','InFlight');

GRANT SELECT, INSERT, UPDATE ON spaceos_sales.sales_outbox TO spaceos_sales_app;
GRANT SELECT, UPDATE ON spaceos_sales.sales_outbox TO spaceos_sales_worker;

ALTER TABLE spaceos_sales.sales_outbox ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_sales.sales_outbox FORCE ROW LEVEL SECURITY;

CREATE POLICY sales_outbox_tenant ON spaceos_sales.sales_outbox
    USING (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid))
    WITH CHECK (""TenantId"" = (current_setting('app.current_tenant_id', true)::uuid));

-- ─── Quote number counters ────────────────────────────────────────────────────
CREATE TABLE spaceos_sales.quote_number_counters (
    tenant_id   uuid    NOT NULL,
    year        integer NOT NULL,
    next_seq    integer NOT NULL DEFAULT 1,
    PRIMARY KEY (tenant_id, year)
);

REVOKE ALL ON spaceos_sales.quote_number_counters FROM PUBLIC;
GRANT SELECT, INSERT, UPDATE ON spaceos_sales.quote_number_counters TO spaceos_sales_app;

-- Grant sequence usage for all sequences in schema
GRANT USAGE ON ALL SEQUENCES IN SCHEMA spaceos_sales TO spaceos_sales_app, spaceos_sales_worker;
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TABLE IF EXISTS spaceos_sales.quote_number_counters CASCADE;
DROP TABLE IF EXISTS spaceos_sales.sales_outbox CASCADE;
DROP TABLE IF EXISTS spaceos_sales.""QuoteLines"" CASCADE;
DROP TABLE IF EXISTS spaceos_sales.""Quotes"" CASCADE;
DROP TABLE IF EXISTS spaceos_sales.""Customers"" CASCADE;
");
    }
}
