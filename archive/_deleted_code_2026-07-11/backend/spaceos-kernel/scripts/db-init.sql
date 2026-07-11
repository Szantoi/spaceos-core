-- SpaceOS DB role separation for Audit Events
-- Runs on first PostgreSQL container startup.
-- spaceos_audit_writer: append-only access to AuditEvents

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_audit_writer') THEN
        -- Password injected at runtime via AUDIT_WRITER_PASSWORD env var.
        -- Example: docker run -e AUDIT_WRITER_PASSWORD=...
        -- Post-create: ALTER ROLE spaceos_audit_writer PASSWORD :'AUDIT_WRITER_PASSWORD';
        CREATE ROLE spaceos_audit_writer LOGIN;
    END IF;
END$$;

-- Grant INSERT + SELECT only on AuditEvents; revoke UPDATE and DELETE
GRANT USAGE ON SCHEMA public TO spaceos_audit_writer;
GRANT SELECT, INSERT ON TABLE "AuditEvents" TO spaceos_audit_writer;
REVOKE UPDATE, DELETE ON TABLE "AuditEvents" FROM spaceos_audit_writer;
REVOKE UPDATE, DELETE ON TABLE "AuditEvents" FROM PUBLIC;

-- The main app role also must not UPDATE/DELETE AuditEvents
-- (enforced at DB level — app-level protection is defense-in-depth)
-- Note: spaceos is the POSTGRES_USER (main app role)
REVOKE UPDATE, DELETE ON TABLE "AuditEvents" FROM spaceos;
