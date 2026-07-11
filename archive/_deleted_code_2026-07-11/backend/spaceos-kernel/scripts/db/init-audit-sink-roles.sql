-- scripts/db/init-audit-sink-roles.sql
-- Creates roles and grants for the spaceos_audit_sink database.
-- Run as a PostgreSQL superuser AFTER the spaceos_audit_sink database has been created
-- and AFTER the Migration_0012_AddHashSinkInfrastructure migration has been applied.
--
-- Usage:
--   psql -U postgres -d spaceos_audit_sink -f scripts/db/init-audit-sink-roles.sql
--
-- DB architecture note:
--   spaceos_audit_sink is a SEPARATE database from spaceos (the main app DB).
--   Compromising the spaceos_app role does not grant access to this sink DB.
--   This is the trust boundary that makes the hash chain tamper-evident.
--
-- Escrow upgrade gate: OFF
--   The PostgreSQL hash sink (two DBs, one instance) is Phase 1.5 baseline.
--   Escrow GA requires upgrading to S3 Object Lock or Azure Immutable Blob.

-- ---------------------------------------------------------------------------
-- 1. spaceos_sink_writer — INSERT only on hash_chain_records
--    Used by: HashSinkDbContext / PostgresHashSink
-- ---------------------------------------------------------------------------
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sink_writer') THEN
    CREATE ROLE spaceos_sink_writer NOLOGIN;
  END IF;
END $$;

-- Create login user for the sink writer (password must be set per environment)
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sink_writer_user') THEN
    CREATE ROLE spaceos_sink_writer_user LOGIN PASSWORD 'CHANGE_ME_IN_VAULT';
  END IF;
END $$;

GRANT spaceos_sink_writer TO spaceos_sink_writer_user;

-- Grant CONNECT on the sink database
GRANT CONNECT ON DATABASE spaceos_audit_sink TO spaceos_sink_writer;

-- Grant USAGE on the schema
GRANT USAGE ON SCHEMA public TO spaceos_sink_writer;

-- INSERT only — no SELECT, no UPDATE, no DELETE
-- SELECT is granted to spaceos_sink_verifier separately
GRANT INSERT ON TABLE hash_chain_records TO spaceos_sink_writer;

-- ---------------------------------------------------------------------------
-- 2. spaceos_sink_verifier — SELECT only on hash_chain_records
--    Used by: verify-chain queries, audit cross-validation
-- ---------------------------------------------------------------------------
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sink_verifier') THEN
    CREATE ROLE spaceos_sink_verifier NOLOGIN;
  END IF;
END $$;

DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sink_verifier_user') THEN
    CREATE ROLE spaceos_sink_verifier_user LOGIN PASSWORD 'CHANGE_ME_IN_VAULT';
  END IF;
END $$;

GRANT spaceos_sink_verifier TO spaceos_sink_verifier_user;

GRANT CONNECT ON DATABASE spaceos_audit_sink TO spaceos_sink_verifier;
GRANT USAGE ON SCHEMA public TO spaceos_sink_verifier;
GRANT SELECT ON TABLE hash_chain_records TO spaceos_sink_verifier;

-- ---------------------------------------------------------------------------
-- 3. Verification
-- ---------------------------------------------------------------------------

-- Expected output:
--   spaceos_sink_writer:   INSERT on hash_chain_records
--   spaceos_sink_verifier: SELECT on hash_chain_records
SELECT
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_name = 'hash_chain_records'
  AND grantee IN ('spaceos_sink_writer', 'spaceos_sink_verifier')
ORDER BY grantee, privilege_type;
