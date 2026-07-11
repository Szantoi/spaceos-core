using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using SpaceOS.Modules.Inventory.Infrastructure.HealthChecks;
using SpaceOS.Modules.Inventory.Infrastructure.Persistence;
using SpaceOS.Modules.Inventory.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Infrastructure;

public class InventoryHealthCheckTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly InventoryDbContext _db;
    private readonly Mock<IWorkerHeartbeatStore> _heartbeatMock;
    private readonly InventoryHealthCheck _sut;
    private readonly HealthCheckContext _ctx = new();

    public InventoryHealthCheckTests()
    {
        // SQLite in-memory supports raw SQL (InMemory provider does not)
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new InventoryDbContext(options);
        _db.Database.EnsureCreated();
        _heartbeatMock = new Mock<IWorkerHeartbeatStore>();
        _sut = new InventoryHealthCheck(_db, _heartbeatMock.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenWorkerHeartbeatRecent_ReturnsHealthy()
    {
        _heartbeatMock
            .Setup(s => s.GetLastTickAsync("inventory-cleanup-worker", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(-5));

        var result = await _sut.CheckHealthAsync(_ctx);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["api"].Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenWorkerHeartbeatStale_ReturnsDegraded()
    {
        _heartbeatMock
            .Setup(s => s.GetLastTickAsync("inventory-cleanup-worker", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(-45));

        var result = await _sut.CheckHealthAsync(_ctx);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("stale");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenWorkerNeverRan_ReturnsHealthy()
    {
        _heartbeatMock
            .Setup(s => s.GetLastTickAsync("inventory-cleanup-worker", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);

        var result = await _sut.CheckHealthAsync(_ctx);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
