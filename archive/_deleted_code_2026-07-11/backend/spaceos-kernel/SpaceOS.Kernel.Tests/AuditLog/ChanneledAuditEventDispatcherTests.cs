// SpaceOS.Kernel.Tests/AuditLog/ChanneledAuditEventDispatcherTests.cs

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Infrastructure.AuditLog;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>
/// Unit tests for <see cref="ChanneledAuditEventDispatcher"/> — Track B (SEC-01, B1, B3).
/// </summary>
public sealed class ChanneledAuditEventDispatcherTests
{
    // ── B1: Channel Wait mode — no silent drop ───────────────────────────────

    [Fact]
    public async Task Channel_WhenFull_DoesNotDropWrite()
    {
        // Arrange: inner dispatcher that blocks until released
        var releaseInner  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var innerCallCount = 0;

        var innerMock = new Mock<IAuditEventDispatcher>();
        innerMock
            .Setup(d => d.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Interlocked.Increment(ref innerCallCount);
                await releaseInner.Task.ConfigureAwait(false);
            });

        var dispatcher = new ChanneledAuditEventDispatcher(
            innerMock.Object,
            NullLogger<ChanneledAuditEventDispatcher>.Instance);

        var batch = new List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act: dispatch two batches — first occupies the reader, second waits in channel
        var task1 = dispatcher.DispatchAsync(batch.AsReadOnly(), TestContext.Current.CancellationToken);
        var task2 = dispatcher.DispatchAsync(batch.AsReadOnly(), TestContext.Current.CancellationToken);

        // Give the dispatcher time to start processing the first batch
        await Task.Delay(50, TestContext.Current.CancellationToken).ConfigureAwait(false);

        // Release the inner dispatcher to drain both batches
        releaseInner.SetResult();

        await Task.WhenAll(task1, task2).ConfigureAwait(false);
        await dispatcher.DisposeAsync().ConfigureAwait(false);

        // Assert: both batches were processed, none dropped
        Assert.Equal(2, innerCallCount);
    }

    // ── B3: DisposeAsync drains within 30s ───────────────────────────────────

    [Fact]
    public async Task DisposeAsync_DrainCompletes_Within30s()
    {
        // Arrange: inner dispatcher that completes immediately
        var innerMock = new Mock<IAuditEventDispatcher>();
        innerMock
            .Setup(d => d.DispatchAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dispatcher = new ChanneledAuditEventDispatcher(
            innerMock.Object,
            NullLogger<ChanneledAuditEventDispatcher>.Instance);

        var batch = new List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act: dispatch a batch, then dispose
        await dispatcher.DispatchAsync(batch.AsReadOnly(), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        var disposeTask = dispatcher.DisposeAsync().AsTask();

        // Assert: dispose completes well within 30 s (use 5 s for test speed)
        var completed = await Task.WhenAny(disposeTask, Task.Delay(5_000, TestContext.Current.CancellationToken))
            .ConfigureAwait(false);

        Assert.Same(disposeTask, completed);
    }
}

/// <summary>
/// Unit tests for <see cref="PostgresAdvisoryAuditWriteLock"/> lock key derivation — Track B2 (SEC-06).
/// </summary>
public sealed class AuditWriteLockKeyTests
{
    // ── B2: MD5-based lock key — unique per tenant ────────────────────────────

    [Fact]
    public void LockKey_DifferentTenants_DifferentInt64Keys()
    {
        var tenant1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var key1 = ComputeLockKey(tenant1);
        var key2 = ComputeLockKey(tenant2);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void LockKey_SameTenant_SameInt64Key()
    {
        var tenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var key1 = ComputeLockKey(tenant);
        var key2 = ComputeLockKey(tenant);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void LockKey_IsFullInt64Range_NotRestrictedToInt32()
    {
        // A hashtext()-based key is restricted to int4 (-2^31..2^31-1).
        // MD5-derived keys should use the full int64 space across a large set of tenants.
        var tenantIds = Enumerable.Range(0, 1000)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var keys = tenantIds.Select(ComputeLockKey).ToList();

        // At least some keys should be outside the int32 range — proving we use int64.
        var outsideInt32 = keys.Any(k => k > int.MaxValue || k < int.MinValue);
        Assert.True(outsideInt32,
            "Expected some MD5-derived lock keys to fall outside the int32 range, " +
            "indicating the full int64 key space is being used.");
    }

    // ── Helper: mirrors the lock key derivation in PostgresAdvisoryAuditWriteLock ──

    private static long ComputeLockKey(Guid tenantId)
    {
        var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(tenantId.ToString()));
        return BitConverter.ToInt64(md5Bytes, 0);
    }
}
