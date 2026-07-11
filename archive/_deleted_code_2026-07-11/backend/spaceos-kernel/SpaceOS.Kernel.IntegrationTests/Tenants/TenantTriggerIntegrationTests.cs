// SpaceOS.Kernel.IntegrationTests/Tenants/TenantTriggerIntegrationTests.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.Tenants;

/// <summary>
/// Integration tests that exercise PostgreSQL-native security triggers defined in Migration 0029.
/// Requires a real PostgreSQL database via <see cref="PostgreSqlFixture"/> (Testcontainers).
/// SQLite does not execute PL/pgSQL triggers — these invariants are silently unverified
/// in the SQLite-backed test suite. This class closes that gap.
/// </summary>
public sealed class TenantTriggerIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>Initialises the test class with the shared PostgreSQL fixture.</summary>
    public TenantTriggerIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    // -------------------------------------------------------------------------
    // SEC-01 — TR_Tenants_ImmutableTenantType
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the SEC-01 PL/pgSQL trigger (<c>TR_Tenants_ImmutableTenantType</c>)
    /// raises a PostgreSQL exception when an UPDATE attempts to change the <c>TenantType</c>
    /// column of an existing tenant. TenantType is immutable after initial creation.
    /// </summary>
    [Fact]
    public async Task SEC01_UpdateTenantType_RaisesException()
    {
        var tenantId = Guid.NewGuid();

        // Arrange: insert a valid Manufacturer tenant
        await _fixture.DbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Tenants" ("Id","Name","TenantType","EnabledModules","IsArchived")
            VALUES ({tenantId},'SEC01-Manufacturer','Manufacturer',ARRAY['door']::varchar(32)[],false)
            """);

        // Act + Assert: UPDATE TenantType fires TR_Tenants_ImmutableTenantType
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _fixture.DbContext.Database.ExecuteSqlAsync(
                $"""UPDATE "Tenants" SET "TenantType" = 'Installer' WHERE "Id" = {tenantId}"""));

        Assert.Contains("immutable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // SEC-02 — TR_Tenants_ValidateModulesForType
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the SEC-02 PL/pgSQL trigger (<c>TR_Tenants_ValidateModulesForType</c>)
    /// raises a PostgreSQL exception when INSERT supplies modules not allowed for the TenantType.
    /// A <c>PanelCutter</c> may only hold the <c>cutting</c> module — <c>orders</c> is invalid.
    /// </summary>
    [Fact]
    public async Task SEC02_Insert_ModulesNotAllowedForTenantType_RaisesException()
    {
        var tenantId = Guid.NewGuid();

        // Act + Assert: PanelCutter with 'orders' fires TR_Tenants_ValidateModulesForType
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _fixture.DbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO "Tenants" ("Id","Name","TenantType","EnabledModules","IsArchived")
                VALUES ({tenantId},'SEC02-BadPanelCutter','PanelCutter',ARRAY['orders']::varchar(32)[],false)
                """));

        Assert.Contains("EnabledModules", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that the SEC-02 trigger also fires on UPDATE.
    /// A <c>Trader</c> may only hold <c>trading</c> and <c>delivery</c>.
    /// Attempting to set <c>door</c> (Manufacturer-only) raises an exception.
    /// </summary>
    [Fact]
    public async Task SEC02_Update_ModulesNotAllowedForTenantType_RaisesException()
    {
        var tenantId = Guid.NewGuid();

        // Arrange: insert a valid Trader tenant
        await _fixture.DbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Tenants" ("Id","Name","TenantType","EnabledModules","IsArchived")
            VALUES ({tenantId},'SEC02-ValidTrader','Trader',ARRAY['trading']::varchar(32)[],false)
            """);

        // Act + Assert: UPDATE with 'door' (not in Trader allowed set) fires SEC-02
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _fixture.DbContext.Database.ExecuteSqlAsync(
                $"""UPDATE "Tenants" SET "EnabledModules" = ARRAY['door']::varchar(32)[] WHERE "Id" = {tenantId}"""));

        Assert.Contains("EnabledModules", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
