// SpaceOS.Kernel.IntegrationTests/AuditLog/HashSinkDbContextTests.cs

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.AuditLog;

/// <summary>
/// Integration tests for <see cref="HashSinkDbContext"/> backed by in-memory SQLite.
/// Verifies the EF Core model, schema creation, and basic insert/read round-trip.
/// </summary>
public sealed class HashSinkDbContextTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private HashSinkDbContext _context = null!;

    /// <summary>Initialises a new test instance with an isolated in-memory SQLite connection.</summary>
    public HashSinkDbContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _context = new HashSinkDbContext(
            new DbContextOptionsBuilder<HashSinkDbContext>()
                .UseSqlite(_connection)
                .Options);

        await _context.Database.EnsureCreatedAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // HashSinkDbContext_EnsureCreated_CreatesHashChainRecordsTable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that EnsureCreated produces a schema that can accept a <see cref="HashChainRecord"/>
    /// insert and return it on a subsequent read.
    /// </summary>
    [Fact]
    public async Task HashSinkDbContext_InsertAndRead_RoundTrip()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var eventId  = Guid.NewGuid();
        var occurred = DateTimeOffset.UtcNow;

        var record = new HashChainRecord
        {
            TenantId   = tenantId,
            EventId    = eventId,
            StateHash  = new string('a', 64),
            OccurredAt = occurred,
            // InsertedAt: left to the DB default (or EF default for SQLite)
        };

        _context.HashChainRecords.Add(record);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        // Act
        var loaded = await _context.HashChainRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EventId == eventId,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(tenantId, loaded.TenantId);
        Assert.Equal(eventId,  loaded.EventId);
        Assert.Equal(new string('a', 64), loaded.StateHash);
        Assert.Equal(occurred.ToUniversalTime(), loaded.OccurredAt.ToUniversalTime());
        Assert.True(loaded.Id > 0); // bigserial assigned
    }

    // -------------------------------------------------------------------------
    // HashSinkDbContext_EventId_IsUnique
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies the UNIQUE constraint on <c>EventId</c> — duplicate sink writes
    /// for the same audit event are rejected by the database.
    /// </summary>
    [Fact]
    public async Task HashSinkDbContext_DuplicateEventId_ThrowsDbUpdateException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        _context.HashChainRecords.Add(new HashChainRecord
        {
            TenantId   = Guid.NewGuid(),
            EventId    = eventId,
            StateHash  = new string('b', 64),
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        // Insert same EventId again
        _context.HashChainRecords.Add(new HashChainRecord
        {
            TenantId   = Guid.NewGuid(),
            EventId    = eventId, // duplicate
            StateHash  = new string('c', 64),
            OccurredAt = DateTimeOffset.UtcNow,
        });

        // Act + Assert — duplicate EventId must be rejected
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    // -------------------------------------------------------------------------
    // HashSinkDbContext_ModelDoesNotIncludeAuditEvent
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="HashSinkDbContext"/> owns only the <c>hash_chain_records</c>
    /// table and does not expose any entities from the main <c>spaceos</c> database.
    /// </summary>
    [Fact]
    public void HashSinkDbContext_ModelContainsOnlyHashChainRecords()
    {
        // Act
        var entityTypes = _context.Model.GetEntityTypes().Select(t => t.ClrType).ToList();

        // Assert — only HashChainRecord; no AuditEvent, Tenant, etc.
        Assert.Single(entityTypes);
        Assert.Contains(typeof(HashChainRecord), entityTypes);
    }
}
