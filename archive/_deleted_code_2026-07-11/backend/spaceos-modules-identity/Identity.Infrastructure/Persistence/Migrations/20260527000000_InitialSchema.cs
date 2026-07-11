// Identity.Infrastructure/Persistence/Migrations/20260527000000_InitialSchema.cs

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Schema
        migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS identity;");

        // Trigger function for updated_at
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION identity.set_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.updated_at = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);

        // spaceos_users
        migrationBuilder.Sql("""
            CREATE TABLE identity.spaceos_users (
                id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
                tenant_id           UUID            NOT NULL,
                email               VARCHAR(254)    NOT NULL,
                first_name          VARCHAR(100)    NOT NULL,
                last_name           VARCHAR(100)    NOT NULL,
                status              VARCHAR(20)     NOT NULL DEFAULT 'Active',
                kc_sync_status      VARCHAR(20)     NOT NULL DEFAULT 'Pending',
                keycloak_user_id    VARCHAR(100)    NULL,
                created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                CONSTRAINT pk_spaceos_users PRIMARY KEY (id),
                CONSTRAINT uq_spaceos_users_email_tenant UNIQUE (email, tenant_id)
            );
            """);

        // Partial UNIQUE: keycloak_user_id WHERE NOT NULL
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX uq_spaceos_users_keycloak_user_id
                ON identity.spaceos_users (keycloak_user_id)
                WHERE keycloak_user_id IS NOT NULL;
            """);

        // Composite index: tenant_id + status
        migrationBuilder.Sql("""
            CREATE INDEX idx_spaceos_users_tenant_status
                ON identity.spaceos_users (tenant_id, status);
            """);

        // Partial index: kc_sync_status (Pending/Failed)
        migrationBuilder.Sql("""
            CREATE INDEX idx_spaceos_users_kc_sync_status
                ON identity.spaceos_users (kc_sync_status)
                WHERE kc_sync_status IN ('Pending', 'Failed');
            """);

        // updated_at trigger
        migrationBuilder.Sql("""
            CREATE TRIGGER trg_spaceos_users_updated_at
                BEFORE UPDATE ON identity.spaceos_users
                FOR EACH ROW EXECUTE FUNCTION identity.set_updated_at();
            """);

        // RLS
        migrationBuilder.Sql("ALTER TABLE identity.spaceos_users ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("ALTER TABLE identity.spaceos_users FORCE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("""
            CREATE POLICY rls_tenant_isolation ON identity.spaceos_users
                USING (tenant_id = current_setting('app.current_tenant_id', TRUE)::UUID);
            """);

        // kc_sync_outbox
        migrationBuilder.Sql("""
            CREATE TABLE identity.kc_sync_outbox (
                id              UUID            NOT NULL DEFAULT gen_random_uuid(),
                user_id         UUID            NOT NULL,
                tenant_id       UUID            NOT NULL,
                operation       VARCHAR(30)     NOT NULL,
                payload         TEXT            NULL,
                attempt_count   INTEGER         NOT NULL DEFAULT 0,
                created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                processed_at    TIMESTAMPTZ     NULL,
                last_attempt_at TIMESTAMPTZ     NULL,
                CONSTRAINT pk_kc_sync_outbox PRIMARY KEY (id),
                CONSTRAINT fk_kc_sync_outbox_user FOREIGN KEY (user_id)
                    REFERENCES identity.spaceos_users(id) ON DELETE CASCADE
            );
            """);

        // Partial index: unprocessed entries
        migrationBuilder.Sql("""
            CREATE INDEX idx_kc_sync_outbox_unprocessed
                ON identity.kc_sync_outbox (created_at)
                WHERE processed_at IS NULL;
            """);

        // audit_log
        migrationBuilder.Sql("""
            CREATE TABLE identity.audit_log (
                id          UUID            NOT NULL DEFAULT gen_random_uuid(),
                tenant_id   UUID            NOT NULL,
                user_id     UUID            NULL,
                action      VARCHAR(100)    NOT NULL,
                target_type VARCHAR(50)     NULL,
                target_id   UUID            NULL,
                payload     TEXT            NULL,
                occurred_at TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                CONSTRAINT pk_audit_log PRIMARY KEY (id)
            );
            """);

        // Composite index: (tenant_id, occurred_at DESC)
        migrationBuilder.Sql("""
            CREATE INDEX idx_audit_log_tenant_occurred
                ON identity.audit_log (tenant_id, occurred_at DESC);
            """);

        // identity_app role: DELETE TILTVA spaceos_users-n
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'identity_app') THEN
                    CREATE ROLE identity_app;
                END IF;
            END
            $$;
            """);
        migrationBuilder.Sql("REVOKE DELETE ON identity.spaceos_users FROM identity_app;");

        // EF migrations history table
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS identity.__efmigrations_history (
                migration_id    VARCHAR(150)    NOT NULL,
                product_version VARCHAR(32)     NOT NULL,
                CONSTRAINT pk_efmigrations_history PRIMARY KEY (migration_id)
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS identity.audit_log CASCADE;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS identity.kc_sync_outbox CASCADE;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS identity.spaceos_users CASCADE;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS identity.set_updated_at() CASCADE;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS identity.__efmigrations_history CASCADE;");
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS identity CASCADE;");
    }
}
