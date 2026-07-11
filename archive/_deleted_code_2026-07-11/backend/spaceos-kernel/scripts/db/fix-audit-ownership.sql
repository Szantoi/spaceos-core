-- scripts/db/fix-audit-ownership.sql
-- SEC-P15-07: Transfer AuditEvents table ownership to the NOLOGIN schema-owner role.
-- Run once as a superuser (e.g. postgres) after the initial schema migration.
--
-- Why a NOLOGIN owner?
--   spaceos_app is a LOGIN role with DML privileges on AuditEvents.
--   PostgreSQL's FORCE ROW LEVEL SECURITY only affects non-owner connections.
--   If spaceos_app owned the table it would bypass RLS entirely.
--   A separate NOLOGIN role owns the table so RLS is enforced for every
--   LOGIN role (spaceos_app, spaceos_audit_writer) without exception.

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_schema_owner') THEN
        -- NOLOGIN: cannot be used as a connection credential.
        CREATE ROLE spaceos_schema_owner NOLOGIN;
    END IF;
END$$;

-- Transfer table ownership to the dedicated schema owner.
-- Only a superuser or the current owner can execute ALTER TABLE ... OWNER TO.
ALTER TABLE "AuditEvents" OWNER TO spaceos_schema_owner;

-- Re-grant DML to the application roles after ownership transfer.
-- (Ownership does not automatically carry over to non-owner roles.)
GRANT USAGE ON SCHEMA public TO spaceos_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "AuditEvents" TO spaceos_app;

GRANT USAGE ON SCHEMA public TO spaceos_audit_writer;
GRANT SELECT, INSERT ON "AuditEvents" TO spaceos_audit_writer;

-- Revoke UPDATE and DELETE from the application role — AuditEvents are append-only.
-- Defense-in-depth: even if application code attempted an update, the DB rejects it.
REVOKE UPDATE, DELETE ON "AuditEvents" FROM spaceos_app;
REVOKE UPDATE, DELETE ON "AuditEvents" FROM spaceos_audit_writer;
REVOKE UPDATE, DELETE ON "AuditEvents" FROM PUBLIC;

-- Enable RLS.
ALTER TABLE "AuditEvents" ENABLE ROW LEVEL SECURITY;

-- FORCE RLS applies the policy even when the session role has table-level privileges.
-- Because spaceos_app is no longer the table owner, FORCE RLS is effective here.
ALTER TABLE "AuditEvents" FORCE ROW LEVEL SECURITY;
