-- scripts/db/init-roles.sql
-- SEC-P15-08: Role creation + RLS policy bootstrapping.
-- Run once as a superuser on first PostgreSQL container startup, before the first migration.
-- Idempotent: every block is guarded with existence checks.

-- -------------------------------------------------------------------------
-- 1. Roles
-- -------------------------------------------------------------------------

DO $$
BEGIN
    -- Schema owner — NOLOGIN, owns tables so that FORCE RLS is effective for
    -- all LOGIN roles (spaceos_app, spaceos_audit_writer).
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_schema_owner') THEN
        CREATE ROLE spaceos_schema_owner NOLOGIN;
    END IF;

    -- Application role — primary DML role used by the SpaceOS.Kernel API.
    -- Password injected at runtime via SPACEOS_APP_PASSWORD env var.
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_app') THEN
        CREATE ROLE spaceos_app LOGIN;
    END IF;

    -- Audit writer role — INSERT + SELECT only on AuditEvents.
    -- Used by the AuditDbContext connection (ConnectionStrings:AuditWriter).
    -- Password injected at runtime via AUDIT_WRITER_PASSWORD env var.
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_audit_writer') THEN
        CREATE ROLE spaceos_audit_writer LOGIN;
    END IF;
END$$;

-- -------------------------------------------------------------------------
-- 2. Schema grants
-- -------------------------------------------------------------------------

GRANT USAGE ON SCHEMA public TO spaceos_app;
GRANT USAGE ON SCHEMA public TO spaceos_audit_writer;

-- -------------------------------------------------------------------------
-- 3. AuditEvents ownership and RLS
--    (Run fix-audit-ownership.sql after first migration to set the OWNER.)
-- -------------------------------------------------------------------------

-- RLS policies — created once, idempotent via DROP IF EXISTS + CREATE.

-- Policy: tenant isolation for SELECT, UPDATE, DELETE.
-- current_setting('app.current_tenant_id', true) returns NULL (not an error)
-- when the GUC is absent, so COALESCE converts it to the nil UUID which
-- will never match a real TenantId — safe fallback for system sessions.
DROP POLICY IF EXISTS audit_tenant_isolation ON "AuditEvents";
CREATE POLICY audit_tenant_isolation ON "AuditEvents"
    USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
            '00000000-0000-0000-0000-000000000000'::uuid
        )
    );

-- Policy: audit_writer_insert_bypass — spaceos_audit_writer is an append-only
-- role that connects with the correct TenantId already set in the session
-- (via SET LOCAL app.current_tenant_id = '...' before INSERT).
-- WITH CHECK (true) allows the INSERT unconditionally for this role;
-- the USING clause of audit_tenant_isolation still governs SELECT visibility.
DROP POLICY IF EXISTS audit_writer_insert_bypass ON "AuditEvents";
CREATE POLICY audit_writer_insert_bypass ON "AuditEvents"
    FOR INSERT
    TO spaceos_audit_writer
    WITH CHECK (true);

-- -------------------------------------------------------------------------
-- 4. Non-AuditEvents table grants
-- -------------------------------------------------------------------------

-- Grant full DML on all other tables to spaceos_app.
-- AuditEvents DML is restricted separately (see fix-audit-ownership.sql).
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO spaceos_app;

-- Revoke UPDATE/DELETE on AuditEvents from spaceos_app (defense-in-depth).
REVOKE UPDATE, DELETE ON "AuditEvents" FROM spaceos_app;
REVOKE UPDATE, DELETE ON "AuditEvents" FROM spaceos_audit_writer;
REVOKE UPDATE, DELETE ON "AuditEvents" FROM PUBLIC;

-- Sequence grants.
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO spaceos_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO spaceos_audit_writer;
