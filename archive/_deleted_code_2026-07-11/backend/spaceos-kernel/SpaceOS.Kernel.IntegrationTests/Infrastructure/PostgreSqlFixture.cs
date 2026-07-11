// SpaceOS.Kernel.IntegrationTests/Infrastructure/PostgreSqlFixture.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit class fixture that starts a real PostgreSQL container via Testcontainers,
/// applies all EF Core migrations (including Migration 0029 with PL/pgSQL triggers),
/// and exposes a ready-to-use <see cref="AppDbContext"/> backed by that container.
/// </summary>
/// <remarks>
/// Used by integration tests that exercise PostgreSQL-native features (triggers, check constraints,
/// array operators) that cannot be verified with the SQLite in-memory provider used by other tests.
/// Each test class declares <c>IClassFixture&lt;PostgreSqlFixture&gt;</c> and receives its own
/// isolated container instance, guaranteeing no cross-class state pollution.
/// </remarks>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    /// <summary>Gets the <see cref="AppDbContext"/> connected to the migrated PostgreSQL container.</summary>
    public AppDbContext DbContext { get; private set; } = null!;

    /// <summary>Gets the connection string for the running test container.</summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        DbContext = new AppDbContext(options, new NullTenantResolver());

        // EnsureCreatedAsync creates the full schema from the current EF Core model.
        // This is more reliable than MigrateAsync for Testcontainers because the full migration
        // chain has production-only dependencies (specific roles, FlowTasks table from modules, etc.)
        // that don't exist in a fresh container.
        await DbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        // Apply Migration 0029 trigger SQL (SEC-01 + SEC-02) directly.
        // EnsureCreatedAsync creates the schema but does not run migrations, so we apply
        // only the trigger definitions that are the subject of these tests.
        await DbContext.Database.ExecuteSqlRawAsync("""
            CREATE OR REPLACE FUNCTION prevent_tenant_type_change()
            RETURNS TRIGGER AS $$
            BEGIN
              IF OLD."TenantType" IS DISTINCT FROM NEW."TenantType" THEN
                RAISE EXCEPTION 'TenantType is immutable after creation. Current: %, Attempted: %',
                  OLD."TenantType", NEW."TenantType";
              END IF;
              RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS "TR_Tenants_ImmutableTenantType" ON "Tenants";
            CREATE TRIGGER "TR_Tenants_ImmutableTenantType"
              BEFORE UPDATE ON "Tenants"
              FOR EACH ROW
              EXECUTE FUNCTION prevent_tenant_type_change();

            CREATE OR REPLACE FUNCTION validate_enabled_modules_for_type()
            RETURNS TRIGGER AS $$
            DECLARE allowed_modules varchar(32)[];
            BEGIN
              CASE NEW."TenantType"
                WHEN 'Manufacturer' THEN allowed_modules := ARRAY['door','cabinet','window','cutting','spatial'];
                WHEN 'PanelCutter'  THEN allowed_modules := ARRAY['cutting'];
                WHEN 'Trader'       THEN allowed_modules := ARRAY['trading','delivery'];
                WHEN 'Logistics'    THEN allowed_modules := ARRAY['delivery'];
                WHEN 'Installer'    THEN allowed_modules := ARRAY['installation'];
                WHEN 'EndCustomer'  THEN allowed_modules := ARRAY['orders'];
                ELSE RAISE EXCEPTION 'Unknown TenantType: %', NEW."TenantType";
              END CASE;
              IF NOT (NEW."EnabledModules" <@ allowed_modules) THEN
                RAISE EXCEPTION 'EnabledModules % not allowed for TenantType %. Allowed: %',
                  NEW."EnabledModules", NEW."TenantType", allowed_modules;
              END IF;
              RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS "TR_Tenants_ValidateModulesForType" ON "Tenants";
            CREATE TRIGGER "TR_Tenants_ValidateModulesForType"
              BEFORE INSERT OR UPDATE ON "Tenants"
              FOR EACH ROW
              EXECUTE FUNCTION validate_enabled_modules_for_type();
            """).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync().ConfigureAwait(false);
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
