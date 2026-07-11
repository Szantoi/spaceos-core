using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace SpaceOS.Modules.Joinery.Tests.Integration;

/// <summary>
/// Testcontainers integration tests for GyartasilapBatch JSONB round-trip.
/// Verifies that _gyartasilapIds is correctly serialised/deserialised via Npgsql on real PostgreSQL.
/// </summary>
[Collection("GyartasilapBatch_Integration")]
public sealed class GyartasilapBatchIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("joinery_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    private JoineryDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsAssembly("SpaceOS.Modules.Joinery.Infrastructure"))
            .Options;

        _db = new JoineryDbContext(opts);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GyartasilapBatch_JsonbRoundTrip_SavesAndLoadsIds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var batchResult = GyartasilapBatch.Create(orderId, tenantId, new List<Guid> { id1, id2 });
        batchResult.IsSuccess.Should().BeTrue();
        var batch = batchResult.Value;

        var repo = new GyartasilapBatchRepository(_db);

        // Act — INSERT
        await repo.AddAsync(batch, CancellationToken.None);

        // Act — SELECT (fresh context to avoid cache)
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var freshDb = new JoineryDbContext(opts);
        var loaded = await freshDb.GyartasilapBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batch.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.GyartasilapIds.Should().HaveCount(2);
        loaded.GyartasilapIds.Should().Contain(id1);
        loaded.GyartasilapIds.Should().Contain(id2);
    }

    [Fact]
    public async Task GyartasilapBatch_JsonbRoundTrip_SingleId_SavesAndLoads()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var singleId = Guid.NewGuid();

        var batchResult = GyartasilapBatch.Create(orderId, tenantId, new List<Guid> { singleId });
        var batch = batchResult.Value;
        var repo = new GyartasilapBatchRepository(_db);

        // Act
        await repo.AddAsync(batch, CancellationToken.None);

        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var freshDb = new JoineryDbContext(opts);
        var loaded = await freshDb.GyartasilapBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batch.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.GyartasilapIds.Should().HaveCount(1);
        loaded.GyartasilapIds[0].Should().Be(singleId);
    }

    [Fact]
    public async Task GyartasilapBatch_StatusTransition_PersistedCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var batchResult = GyartasilapBatch.Create(orderId, tenantId, new List<Guid> { Guid.NewGuid() });
        var batch = batchResult.Value;
        var repo = new GyartasilapBatchRepository(_db);
        await repo.AddAsync(batch, CancellationToken.None);

        // Act — transition to Ready
        batch.MarkGenerating();
        batch.MarkReady("gyartasilap/batch/test.zip");
        await repo.UpdateAsync(batch, CancellationToken.None);

        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var freshDb = new JoineryDbContext(opts);
        var loaded = await freshDb.GyartasilapBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batch.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(BatchStatus.Ready);
        loaded.ZipStoragePath.Should().Be("gyartasilap/batch/test.zip");
        loaded.GyartasilapIds.Should().HaveCount(1);
    }
}
