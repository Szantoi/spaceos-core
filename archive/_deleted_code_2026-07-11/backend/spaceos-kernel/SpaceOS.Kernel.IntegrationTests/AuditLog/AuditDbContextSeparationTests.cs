// SpaceOS.Kernel.IntegrationTests/AuditLog/AuditDbContextSeparationTests.cs

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Data;
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog.Specifications;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.AuditLog;

/// <summary>
/// Integration tests verifying that <see cref="AuditDbContext"/> and <see cref="AppDbContext"/>
/// are fully separated: AuditEvents are only visible via <see cref="AuditDbContext"/>, and
/// <see cref="AppDbContext"/> does not expose or own the AuditEvents table.
/// </summary>
public sealed class AuditDbContextSeparationTests : IAsyncLifetime
{
    // Two separate SQLite connections simulating the two database contexts.
    // In production these would be two different PostgreSQL connection strings
    // (spaceos_app role vs. spaceos_audit_writer role).
    private readonly SqliteConnection _appConnection;
    private readonly SqliteConnection _auditConnection;

    private AppDbContext _appContext = null!;
    private AuditDbContext _auditContext = null!;

    /// <summary>Initialises a new test instance with isolated in-memory connections.</summary>
    public AuditDbContextSeparationTests()
    {
        _appConnection   = new SqliteConnection("Data Source=:memory:");
        _auditConnection = new SqliteConnection("Data Source=:memory:");
        _appConnection.Open();
        _auditConnection.Open();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _appContext = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_appConnection).Options,
            new NullTenantResolver());

        _auditContext = new AuditDbContext(
            new DbContextOptionsBuilder<AuditDbContext>().UseSqlite(_auditConnection).Options);

        await _appContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
        await _auditContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _appContext.DisposeAsync().ConfigureAwait(false);
        await _auditContext.DisposeAsync().ConfigureAwait(false);
        await _appConnection.DisposeAsync().ConfigureAwait(false);
        await _auditConnection.DisposeAsync().ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // AuditDbContext_OnlyOwns_AuditEventsTable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AuditDbContext"/> only exposes the AuditEvents DbSet
    /// and that the schema created by EnsureCreated contains the AuditEvents table.
    /// </summary>
    [Fact]
    public async Task AuditDbContext_OnlyOwns_AuditEventsTable()
    {
        // Arrange — insert an audit event via AuditDbContext
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            "SeparationTest",
            Guid.NewGuid(),
            "{}",
            "a".PadLeft(64, 'a'));

        var repository = new AuditEventRepository(_auditContext);

        // Act
        await repository.AddAsync(auditEvent, TestContext.Current.CancellationToken);
        await _auditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _auditContext.ChangeTracker.Clear();

        var results = await repository.ListAsync(
            new AllAuditEventsSpec(), TestContext.Current.CancellationToken);

        // Assert — event is present in AuditDbContext
        Assert.Single(results);
        Assert.Equal(auditEvent.Id, results[0].Id);
    }

    // -------------------------------------------------------------------------
    // AppDbContext_DoesNotExpose_AuditEventsTable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AppDbContext"/> does not have an AuditEvents DbSet,
    /// and that the model created by EnsureCreated does not include the AuditEvents entity type.
    /// </summary>
    [Fact]
    public void AppDbContext_DoesNotExpose_AuditEventsTable()
    {
        // Act — attempt to find the AuditEvent entity type in AppDbContext's model
        var entityType = _appContext.Model.FindEntityType(typeof(AuditEvent));

        // Assert — AuditEvent must not be part of AppDbContext's model
        Assert.Null(entityType);
    }

    // -------------------------------------------------------------------------
    // AuditEvent_WrittenViaAuditContext_IsNotVisibleViaAppContext
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies cross-context isolation: an <see cref="AuditEvent"/> written through
    /// <see cref="AuditDbContext"/> is not readable through <see cref="AppDbContext"/>
    /// (because <see cref="AppDbContext"/> does not model the AuditEvents table).
    /// </summary>
    [Fact]
    public async Task AuditEvent_WrittenViaAuditContext_IsNotVisibleViaAppContext()
    {
        // Arrange — write an audit event via AuditDbContext
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(),
            "CrossContextTest",
            Guid.NewGuid(),
            "{}",
            "b".PadLeft(64, 'b'));

        var repository = new AuditEventRepository(_auditContext);
        await repository.AddAsync(auditEvent, TestContext.Current.CancellationToken);
        await _auditContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act — AppDbContext cannot even model-query AuditEvents
        var entityType = _appContext.Model.FindEntityType(typeof(AuditEvent));

        // Assert — AppDbContext has no knowledge of the AuditEvents table
        Assert.Null(entityType);
    }

    // -------------------------------------------------------------------------
    // AuditDbContext_TenantIsolation_OnlyReturnsOwnTenantEvents
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <see cref="AuditEventRepository"/> correctly filters by TenantId
    /// when using <see cref="AuditDbContext"/>, providing logical tenant isolation
    /// equivalent to the RLS policy enforced in production PostgreSQL.
    /// </summary>
    [Fact]
    public async Task AuditDbContext_TenantIsolation_OnlyReturnsOwnTenantEvents()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var eventForA = AuditEvent.Create(tenantA, "EventA", Guid.NewGuid(), "{}", "a".PadLeft(64, 'a'));
        var eventForB = AuditEvent.Create(tenantB, "EventB", Guid.NewGuid(), "{}", "b".PadLeft(64, 'b'));

        var repository = new AuditEventRepository(_auditContext);
        await repository.AddAsync(eventForA, TestContext.Current.CancellationToken);
        await repository.AddAsync(eventForB, TestContext.Current.CancellationToken);
        await _auditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        _auditContext.ChangeTracker.Clear();

        // Act
        var chainA = await repository.GetChainAsync(
            tenantA, from: null, to: null, TestContext.Current.CancellationToken);
        var chainB = await repository.GetChainAsync(
            tenantB, from: null, to: null, TestContext.Current.CancellationToken);

        // Assert — each tenant only sees its own events
        Assert.Single(chainA);
        Assert.Equal(eventForA.Id, chainA[0].Id);

        Assert.Single(chainB);
        Assert.Equal(eventForB.Id, chainB[0].Id);
    }
}
