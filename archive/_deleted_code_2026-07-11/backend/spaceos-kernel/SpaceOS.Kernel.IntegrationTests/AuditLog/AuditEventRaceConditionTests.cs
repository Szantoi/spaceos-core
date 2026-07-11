// SpaceOS.Kernel.IntegrationTests/AuditLog/AuditEventRaceConditionTests.cs

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Infrastructure.AuditLog;
using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Infrastructure.Persistence;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.AuditLog;

/// <summary>
/// Race-condition load tests for <see cref="AuditEventDispatcher"/> against the
/// <see cref="InProcessAuditWriteLock"/> advisory lock.
///
/// <para>
/// The test fires 50 concurrent dispatches through <see cref="AuditEventDispatcher"/>
/// and then asserts:
/// <list type="bullet">
///   <item>No two events share the same <c>PreviousHash</c> value — proof that the
///   per-tenant semaphore serialised every append.</item>
///   <item>The 50 events form a single, unbroken linked list anchored at the genesis hash.</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Connection strategy:</strong> A single <see cref="SqliteConnection"/> is kept open
/// for the test lifetime and shared across all concurrent dispatchers via
/// <c>UseSqlite(_connection)</c>. Each dispatcher gets its own <see cref="AuditDbContext"/>
/// (and therefore its own EF Core change tracker), but all contexts use the same underlying
/// physical connection. The <see cref="InProcessAuditWriteLock"/> semaphore guarantees that at
/// most one context performs a DB operation at any instant — so there is never concurrent EF
/// Core activity on the connection. This avoids both the EF Core concurrency-detection error
/// (shared <c>DbContext</c>) and the SQLite stale-read problem (independent connections).
/// </para>
/// </summary>
public sealed class AuditEventRaceConditionTests : IAsyncLifetime
{
    private const int ConcurrentWriteCount = 50;
    private const string GenesisHash       = "0000000000000000000000000000000000000000000000000000000000000000";

    // Shared physical SQLite connection — kept open for the test lifetime to preserve
    // the in-memory schema. All AuditDbContext instances use this connection.
    private readonly SqliteConnection _connection;

    /// <summary>Creates a fresh in-memory SQLite connection for this test instance.</summary>
    public AuditEventRaceConditionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await using var ctx = new AuditDbContext(BuildOptions());
        await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // ConcurrentDispatch_NoPreviousHashDuplicates
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fires 50 concurrent <see cref="AuditEventDispatcher.DispatchAsync"/> calls for the same
    /// tenant and verifies that no two events share the same <c>PreviousHash</c> value.
    /// A duplicate <c>PreviousHash</c> would indicate a forked chain caused by a race condition
    /// where two dispatchers concurrently read the same tail hash.
    /// </summary>
    [Fact]
    public async Task ConcurrentDispatch_NoPreviousHashDuplicates()
    {
        // Arrange
        var tenantId        = Guid.NewGuid();
        var writeLock       = new InProcessAuditWriteLock();
        var genesisProvider = new FixedGenesisHashProvider(GenesisHash);
        var sink            = new NullExternalAuditSink();
        var hashProvider    = new Sha256HashProvider();
        var requestContext  = new NullRequestContext();

        // Each task creates its own AuditDbContext (own change tracker), but all share
        // the same SqliteConnection. The InProcessAuditWriteLock semaphore ensures that
        // only one context performs DB operations at a time, preventing the EF Core
        // concurrency-detection error that would occur with concurrent same-context access.
        var tasks = Enumerable.Range(0, ConcurrentWriteCount).Select(_ => Task.Run(async () =>
        {
            await using var ctx = new AuditDbContext(BuildOptions());
            var repository    = new AuditEventRepository(ctx);
            var unitOfWork    = new AuditUnitOfWork(ctx);
            var pseudonymizer = new NullPseudonymizer();

            var dispatcher = new AuditEventDispatcher(
                repository,
                unitOfWork,
                requestContext,
                writeLock,
                sink,
                genesisProvider,
                hashProvider,
                pseudonymizer,
                new NullAuditEscrowWriter());

            var domainEvent = new TenantCreatedEvent(
                TenantId.From(tenantId),
                DateTimeOffset.UtcNow);

            await dispatcher.DispatchAsync(
                new IDomainEvent[] { domainEvent },
                TestContext.Current.CancellationToken);
        }));

        // Act
        await Task.WhenAll(tasks);

        // Assert — load all persisted events and verify PreviousHash uniqueness.
        await using var readCtx = new AuditDbContext(BuildOptions());
        var readRepo  = new AuditEventRepository(readCtx);
        var allEvents = await readRepo.GetChainAsync(
            tenantId, from: null, to: null, TestContext.Current.CancellationToken);

        Assert.Equal(ConcurrentWriteCount, allEvents.Count);

        var previousHashes = allEvents.Select(e => e.PreviousHash).ToList();
        var distinctCount  = previousHashes.Distinct(StringComparer.Ordinal).Count();

        // Every PreviousHash must be unique — a duplicate means two events were
        // appended using the same tail hash, i.e., the chain forked.
        Assert.Equal(ConcurrentWriteCount, distinctCount);
    }

    // -------------------------------------------------------------------------
    // ConcurrentDispatch_VerifyChainIsValid
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fires 50 concurrent dispatches for the same tenant and then verifies that the
    /// resulting audit events form a single, unforked linked list where every
    /// <c>PreviousHash</c> links exactly one event forward to the next event's <c>StateHash</c>.
    ///
    /// <para>
    /// The chain is verified by following <c>PreviousHash → StateHash</c> pointers from the
    /// genesis anchor — not by relying on <c>OccurredAt</c> ordering, which is non-deterministic
    /// when multiple events share the same millisecond timestamp (as is common in a load test).
    /// </para>
    /// </summary>
    [Fact]
    public async Task ConcurrentDispatch_VerifyChainIsValid()
    {
        // Arrange
        var tenantId        = Guid.NewGuid();
        var writeLock       = new InProcessAuditWriteLock();
        var genesisProvider = new FixedGenesisHashProvider(GenesisHash);
        var sink            = new NullExternalAuditSink();
        var hashProvider    = new Sha256HashProvider();
        var requestContext  = new NullRequestContext();

        var tasks = Enumerable.Range(0, ConcurrentWriteCount).Select(_ => Task.Run(async () =>
        {
            await using var ctx = new AuditDbContext(BuildOptions());
            var repository    = new AuditEventRepository(ctx);
            var unitOfWork    = new AuditUnitOfWork(ctx);
            var pseudonymizer = new NullPseudonymizer();

            var dispatcher = new AuditEventDispatcher(
                repository,
                unitOfWork,
                requestContext,
                writeLock,
                sink,
                genesisProvider,
                hashProvider,
                pseudonymizer,
                new NullAuditEscrowWriter());

            var domainEvent = new TenantCreatedEvent(
                TenantId.From(tenantId),
                DateTimeOffset.UtcNow);

            await dispatcher.DispatchAsync(
                new IDomainEvent[] { domainEvent },
                TestContext.Current.CancellationToken);
        }));

        // Act
        await Task.WhenAll(tasks);

        // Assert — walk the chain as a linked list.
        // Each PreviousHash must map to exactly one StateHash — no forks, no cycles.
        // This does not depend on OccurredAt ordering, which is non-deterministic when
        // events share the same millisecond timestamp under a 50-concurrent-writer load.
        await using var readCtx = new AuditDbContext(BuildOptions());
        var readRepo  = new AuditEventRepository(readCtx);
        var allEvents = await readRepo.GetChainAsync(
            tenantId, from: null, to: null, TestContext.Current.CancellationToken);

        Assert.Equal(ConcurrentWriteCount, allEvents.Count);

        // Build a forward map: previousHash → stateHash.
        // A forked chain would produce two entries with the same previousHash key.
        var forwardMap = new Dictionary<string, string>(ConcurrentWriteCount, StringComparer.Ordinal);
        foreach (var e in allEvents)
        {
            Assert.False(forwardMap.ContainsKey(e.PreviousHash),
                $"Forked chain detected: two events share PreviousHash '{e.PreviousHash}'.");
            forwardMap[e.PreviousHash] = e.StateHash;
        }

        // Walk the linked list from genesis — must visit exactly all 50 events.
        var visited = 0;
        var current = GenesisHash;
        while (forwardMap.TryGetValue(current, out var next))
        {
            current = next;
            visited++;
        }

        Assert.Equal(ConcurrentWriteCount, visited);
    }

    // -------------------------------------------------------------------------
    // ConcurrentDispatch_VerifyChainQueryReturnsIsValid
    // -------------------------------------------------------------------------

    /// <summary>
    /// KERNEL-070: Fires 50 concurrent dispatches for the same tenant, then executes
    /// <see cref="VerifyChainQueryHandler"/> to assert <c>IsValid = true</c>.
    /// This is the end-to-end proof that the advisory lock fix prevents hash chain
    /// mismatch under concurrent writes — the exact scenario that broke verify-chain
    /// in production.
    /// </summary>
    [Fact]
    public async Task ConcurrentDispatch_VerifyChainQueryReturnsIsValid()
    {
        // Arrange
        var tenantId        = Guid.NewGuid();
        var writeLock       = new InProcessAuditWriteLock();
        var genesisProvider = new FixedGenesisHashProvider(GenesisHash);
        var sink            = new NullExternalAuditSink();
        var hashProvider    = new Sha256HashProvider();
        var requestContext  = new NullRequestContext();

        var tasks = Enumerable.Range(0, ConcurrentWriteCount).Select(_ => Task.Run(async () =>
        {
            await using var ctx = new AuditDbContext(BuildOptions());
            var repository    = new AuditEventRepository(ctx);
            var unitOfWork    = new AuditUnitOfWork(ctx);
            var pseudonymizer = new NullPseudonymizer();

            var dispatcher = new AuditEventDispatcher(
                repository,
                unitOfWork,
                requestContext,
                writeLock,
                sink,
                genesisProvider,
                hashProvider,
                pseudonymizer,
                new NullAuditEscrowWriter());

            var domainEvent = new TenantCreatedEvent(
                TenantId.From(tenantId),
                DateTimeOffset.UtcNow);

            await dispatcher.DispatchAsync(
                new IDomainEvent[] { domainEvent },
                TestContext.Current.CancellationToken);
        }));

        // Act — dispatch all events concurrently
        await Task.WhenAll(tasks);

        // Assert — run VerifyChainQueryHandler and confirm isValid=true
        await using var readCtx = new AuditDbContext(BuildOptions());
        var readRepo     = new AuditEventRepository(readCtx);
        var proofStorage = new NullProofStorageService();
        var handler      = new VerifyChainQueryHandler(readRepo, sink, genesisProvider, proofStorage);

        var query  = new VerifyChainQuery(tenantId, From: null, To: null);
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        var dto = result.Value;
        Assert.True(dto.IsValid, $"Chain verification failed: first broken at {dto.FirstBrokenAt}");
        Assert.Equal(ConcurrentWriteCount, dto.TotalRecordsChecked);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private DbContextOptions<AuditDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(_connection)
            .Options;

    // -------------------------------------------------------------------------
    // Local stubs — scoped to this test class only
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a fixed, constant genesis hash.
    /// Avoids configuration dependencies in the load test.
    /// </summary>
    private sealed class FixedGenesisHashProvider(string hash) : IGenesisHashProvider
    {
        /// <inheritdoc/>
        public Task<string> GetGenesisHashAsync(CancellationToken ct = default) =>
            Task.FromResult(hash);
    }

    /// <summary>
    /// No-op external audit sink. The load test only verifies primary-store chain integrity;
    /// cross-sink validation is not the subject of this test.
    /// </summary>
    private sealed class NullExternalAuditSink : IExternalAuditSink
    {
        /// <inheritdoc/>
        public Task WriteAsync(
            Guid tenantId,
            string eventType,
            string stateHash,
            string previousHash,
            DateTimeOffset occurredAt,
            CancellationToken ct = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<IReadOnlyList<ExternalAuditHashRecord>> ReadHashesAsync(
            Guid tenantId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ExternalAuditHashRecord>>(Array.Empty<ExternalAuditHashRecord>());
    }

    /// <summary>
    /// Returns null for all request context properties.
    /// Prevents the dispatcher from attempting pseudonymization, which would
    /// require a full <see cref="IUserProfileRepository"/> dependency.
    /// </summary>
    private sealed class NullRequestContext : ICurrentRequestContext
    {
        /// <inheritdoc/>
        public string? ActorId => null;

        /// <inheritdoc/>
        public string? SourceIp => null;

        /// <inheritdoc/>
        public string? SourceBrand => null;
    }

    /// <summary>
    /// Pseudonymizer stub — never called because <see cref="NullRequestContext.ActorId"/>
    /// is always null. Included to satisfy the <see cref="AuditEventDispatcher"/> constructor.
    /// </summary>
    private sealed class NullPseudonymizer : IPseudonymizer
    {
        /// <inheritdoc/>
        public Task<Guid> GetOrCreatePseudonymAsync(
            string externalUserId,
            Guid tenantId,
            CancellationToken ct = default) =>
            Task.FromResult(Guid.NewGuid());
    }

    /// <summary>
    /// Proof storage stub — always available, no real storage backend.
    /// Used to satisfy <see cref="VerifyChainQueryHandler"/> constructor.
    /// </summary>
    private sealed class NullProofStorageService : IProofStorageService
    {
        /// <inheritdoc/>
        public string ProviderName => "null";

        /// <inheritdoc/>
        public Task<(string Hash, string StorageKey)> UploadAsync(
            Stream content, string fileName, string contentType,
            Guid tenantId, CancellationToken ct) =>
            Task.FromResult(("0", "null"));

        /// <inheritdoc/>
        public Task<bool> VerifyHashAsync(string storageKey, string expectedHash, CancellationToken ct) =>
            Task.FromResult(true);

        /// <inheritdoc/>
        public Task<bool> IsAvailableAsync(CancellationToken ct) =>
            Task.FromResult(true);
    }
}
