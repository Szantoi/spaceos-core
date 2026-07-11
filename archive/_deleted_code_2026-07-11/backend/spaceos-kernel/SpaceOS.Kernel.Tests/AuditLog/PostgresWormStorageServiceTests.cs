// SpaceOS.Kernel.Tests/AuditLog/PostgresWormStorageServiceTests.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Infrastructure.Storage;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>
/// Unit tests for <see cref="PostgresWormStorageService"/> — Track C (SEC-03 / SEC-07).
/// These tests verify the service's constructor guard and interface contract without
/// making real database connections (integration tests are separate).
/// </summary>
public sealed class PostgresWormStorageServiceTests
{
    // ── C: Construction guards ────────────────────────────────────────────────

    [Fact]
    public void Constructor_WhenConnectionStringMissing_Throws()
    {
        // Arrange: empty configuration — AUDIT_SINK_CONNECTION_STRING not set
        var config = new ConfigurationBuilder().Build();

        // Act + Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PostgresWormStorageService(config, NullLogger<PostgresWormStorageService>.Instance));

        Assert.Contains("AUDIT_SINK_CONNECTION_STRING", ex.Message);
    }

    [Fact]
    public void Constructor_WhenConnectionStringPresent_DoesNotThrow()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUDIT_SINK_CONNECTION_STRING"] = "Host=localhost;Database=test;Username=worm;Password=secret"
            })
            .Build();

        // Act + Assert: no exception — service constructed successfully
        var svc = new PostgresWormStorageService(config, NullLogger<PostgresWormStorageService>.Instance);
        Assert.IsAssignableFrom<IWormStorageService>(svc);
    }

    [Fact]
    public void PostgresWormStorageService_Implements_IWormStorageService()
    {
        // Verify interface implementation — Track C interface contract
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUDIT_SINK_CONNECTION_STRING"] = "Host=localhost;Database=test;Username=worm;Password=secret"
            })
            .Build();

        var svc = new PostgresWormStorageService(config, NullLogger<PostgresWormStorageService>.Instance);

        Assert.IsAssignableFrom<IWormStorageService>(svc);
    }

    [Fact]
    public async Task AppendAsync_WhenHashIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUDIT_SINK_CONNECTION_STRING"] = "Host=localhost;Database=test;Username=worm;Password=secret"
            })
            .Build();

        var svc = new PostgresWormStorageService(config, NullLogger<PostgresWormStorageService>.Instance);

        // Act + Assert: empty hash must be rejected before hitting the database
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.AppendAsync(Guid.NewGuid(), 1L, string.Empty, CancellationToken.None));
    }

    // ── C: Migration 0027 — class existence and suppressTransaction ───────────

    [Fact]
    public void Migration0027_ClassExists_WithCorrectName()
    {
        // Confirm the migration type is discoverable by EF Core tooling.
        var migrationType = typeof(SpaceOS.Infrastructure.Migrations.Migration_0027_AuditHashesWorm);

        Assert.NotNull(migrationType);
        Assert.Equal("Migration_0027_AuditHashesWorm", migrationType.Name);
    }

    // ── C: RLS — policy exists in migration SQL ───────────────────────────────

    [Fact]
    public void Migration0027_UpSql_ContainsRlsPolicy()
    {
        // Verify the migration SQL contains the RLS POLICY directive.
        // We check via reflection on the migration source since we cannot run it without a real PG server.
        var source = System.IO.File.ReadAllText(
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",  // back to repo root
                "SpaceOS.Infrastructure", "Migrations",
                "20260408120000_Migration_0027_AuditHashesWorm.cs"));

        Assert.Contains("ENABLE ROW LEVEL SECURITY", source);
        Assert.Contains("CREATE POLICY", source);
        Assert.Contains("rls_audit_hashes_tenant", source);
    }

    [Fact]
    public void Migration0027_UpSql_ContainsSuppressTransaction()
    {
        var source = System.IO.File.ReadAllText(
            System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "SpaceOS.Infrastructure", "Migrations",
                "20260408120000_Migration_0027_AuditHashesWorm.cs"));

        Assert.Contains("suppressTransaction: true", source);
    }
}
