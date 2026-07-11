// SpaceOS.Infrastructure/Data/PostgresSchemaInitializer.cs
using Microsoft.EntityFrameworkCore;

namespace SpaceOS.Infrastructure.Data;

/// <summary>
/// Applies PostgreSQL-specific schema objects (security_barrier views) after <c>EnsureCreated</c>.
/// This class is a no-op when the active provider is not PostgreSQL, making it safe to call
/// in all environments including SQLite-backed development and test runs.
/// </summary>
/// <remarks>
/// DB-06 (JSONB CHECK constraints) and DB-09 (RLS policies) are complex, environment-specific
/// DDL that cannot be expressed portably via EF Core. Those must be applied via versioned
/// PostgreSQL migration scripts executed as an explicit operational step.
/// </remarks>
public static class PostgresSchemaInitializer
{
    /// <summary>
    /// Applies PostgreSQL-specific DDL to the database reachable through <paramref name="context"/>.
    /// Idempotent — uses <c>CREATE OR REPLACE VIEW</c> so repeated calls are safe.
    /// </summary>
    /// <param name="context">The <see cref="AppDbContext"/> connected to the target database.</param>
    /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
    public static async Task ApplyAsync(AppDbContext context, CancellationToken ct = default)
    {
        if (!context.Database.IsNpgsql()) return;

        // DB-05: security_barrier views
        // v_handshake_delivery — exposes only epics that carry delivery-visible handshake data.
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE OR REPLACE VIEW v_handshake_delivery WITH (security_barrier = true) AS
            SELECT fe.""Id"", fe.""TenantId"", fe.""Handshake_GuestTenantId"", fe.""Handshake_VisibilityScope""
            FROM ""FlowEpics"" fe
            WHERE fe.""Handshake_VisibilityScope"" IN ('DeliveryOnly', 'FullTransparent');
        ", ct).ConfigureAwait(false);

        // v_handshake_spec — exposes only epics where the specification has been shared.
        await context.Database.ExecuteSqlRawAsync(@"
            CREATE OR REPLACE VIEW v_handshake_spec WITH (security_barrier = true) AS
            SELECT fe.""Id"", fe.""TenantId"", fe.""Handshake_GuestTenantId"", fe.""Handshake_VisibilityScope""
            FROM ""FlowEpics"" fe
            WHERE fe.""Handshake_VisibilityScope"" IN ('SpecShared', 'FullTransparent');
        ", ct).ConfigureAwait(false);

        // Sprint C schema fix: SprintC_SchemaUpdate migration used SQLite-compatible TEXT type
        // for UUID and timestamptz columns. Npgsql 8 sends typed uuid/timestamptz parameters,
        // which PostgreSQL rejects with "operator does not exist: text = uuid".
        // These DO blocks are idempotent — only execute if the column type is still 'text'.

        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'AggregateSnapshots' AND column_name = 'Id' AND data_type = 'text'
              ) THEN
                ALTER TABLE ""AggregateSnapshots""
                  ALTER COLUMN ""Id""             TYPE uuid        USING ""Id""::uuid,
                  ALTER COLUMN ""AggregateId""    TYPE uuid        USING ""AggregateId""::uuid,
                  ALTER COLUMN ""TenantId""       TYPE uuid        USING ""TenantId""::uuid,
                  ALTER COLUMN ""SnapshotAt""     TYPE timestamptz USING ""SnapshotAt""::timestamptz;
                IF EXISTS (
                  SELECT 1 FROM information_schema.columns
                  WHERE table_name = 'AggregateSnapshots' AND column_name = 'TriggerEventId' AND data_type = 'text'
                ) THEN
                  ALTER TABLE ""AggregateSnapshots""
                    ALTER COLUMN ""TriggerEventId"" TYPE uuid USING ""TriggerEventId""::uuid;
                END IF;
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'NodeManifests' AND column_name = 'Id' AND data_type = 'text'
              ) THEN
                ALTER TABLE ""NodeManifests""
                  ALTER COLUMN ""Id""               TYPE uuid        USING ""Id""::uuid,
                  ALTER COLUMN ""TenantId""         TYPE uuid        USING ""TenantId""::uuid,
                  ALTER COLUMN ""LastHeartbeatAt""  TYPE timestamptz USING ""LastHeartbeatAt""::timestamptz,
                  ALTER COLUMN ""CreatedAt""        TYPE timestamptz USING ""CreatedAt""::timestamptz;
                IF EXISTS (
                  SELECT 1 FROM information_schema.columns
                  WHERE table_name = 'NodeManifests' AND column_name = 'UpdatedAt' AND data_type = 'text'
                ) THEN
                  ALTER TABLE ""NodeManifests""
                    ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING ""UpdatedAt""::timestamptz;
                END IF;
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'OutboxMessages' AND column_name = 'Id' AND data_type = 'text'
              ) THEN
                ALTER TABLE ""OutboxMessages""
                  ALTER COLUMN ""Id""          TYPE uuid        USING ""Id""::uuid,
                  ALTER COLUMN ""TenantId""    TYPE uuid        USING ""TenantId""::uuid,
                  ALTER COLUMN ""CreatedAt""   TYPE timestamptz USING ""CreatedAt""::timestamptz;
                IF EXISTS (
                  SELECT 1 FROM information_schema.columns
                  WHERE table_name = 'OutboxMessages' AND column_name = 'ProcessedAt' AND data_type = 'text'
                ) THEN
                  ALTER TABLE ""OutboxMessages""
                    ALTER COLUMN ""ProcessedAt"" TYPE timestamptz USING ""ProcessedAt""::timestamptz;
                END IF;
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'SyncSignals' AND column_name = 'Id' AND data_type = 'text'
              ) THEN
                ALTER TABLE ""SyncSignals""
                  ALTER COLUMN ""Id""             TYPE uuid        USING ""Id""::uuid,
                  ALTER COLUMN ""EpicId""         TYPE uuid        USING ""EpicId""::uuid,
                  ALTER COLUMN ""TenantId""       TYPE uuid        USING ""TenantId""::uuid,
                  ALTER COLUMN ""ClientSignalId"" TYPE uuid        USING ""ClientSignalId""::uuid,
                  ALTER COLUMN ""ExpiresAt""      TYPE timestamptz USING ""ExpiresAt""::timestamptz,
                  ALTER COLUMN ""OccurredAt""     TYPE timestamptz USING ""OccurredAt""::timestamptz;
              END IF;
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'SyncSignals' AND column_name = 'IsSyncedToKernel' AND data_type = 'integer'
              ) THEN
                ALTER TABLE ""SyncSignals""
                  ALTER COLUMN ""IsSyncedToKernel"" TYPE boolean USING (""IsSyncedToKernel"" != 0);
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'UserProfiles' AND column_name = 'Id' AND data_type = 'text'
              ) THEN
                ALTER TABLE ""UserProfiles""
                  ALTER COLUMN ""Id""        TYPE uuid        USING ""Id""::uuid,
                  ALTER COLUMN ""TenantId""  TYPE uuid        USING ""TenantId""::uuid,
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING ""CreatedAt""::timestamptz;
              END IF;
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'UserProfiles' AND column_name = 'IsErased' AND data_type = 'integer'
              ) THEN
                ALTER TABLE ""UserProfiles""
                  ALTER COLUMN ""IsErased"" TYPE boolean USING (""IsErased"" != 0);
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        // Migration 0024: BrandSkinId on Tenants — idempotent ADD COLUMN IF NOT EXISTS.
        // Required because the Kernel EF model expects this column; without it every
        // SELECT on Tenants fails with PostgresException 42703 "column does not exist".
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = 'Tenants' AND column_name = 'BrandSkinId'
              ) THEN
                ALTER TABLE ""Tenants""
                  ADD COLUMN ""BrandSkinId"" character varying(64) NULL;
              END IF;
            END $$;
        ", ct).ConfigureAwait(false);

        // Ensure migration 0024 is recorded in __EFMigrationsHistory so MigrateAsync() skips it.
        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20260407190000_Migration_0024_TenantsBrandSkinId', '8.0.11')
            ON CONFLICT DO NOTHING;
        ", ct).ConfigureAwait(false);
    }
}
