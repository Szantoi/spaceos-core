// SpaceOS.Kernel.IntegrationTests/AuditLog/AuditChainIntegrityTest.cs

using SpaceOS.Infrastructure.Data.Repositories;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.IntegrationTests.Infrastructure;
using Xunit;

namespace SpaceOS.Kernel.IntegrationTests.AuditLog;

/// <summary>
/// Integration tests that verify the SHA-256 audit chain integrity invariant as a CI gate.
///
/// <para>
/// <b>R-15 / KERNEL-070 root cause (preexisting gap):</b><br/>
/// The audit chain correctness — each event's <see cref="AuditEvent.PreviousHash"/> must equal
/// the preceding event's <see cref="AuditEvent.StateHash"/> — was maintained by convention
/// via <see cref="SpaceOS.Kernel.Application.AuditLog.AuditEventDispatcher"/> but had no
/// automated CI gate to detect chain breaks.
/// </para>
/// <para>
/// <b>Known ordering limitation:</b><br/>
/// <see cref="SpaceOS.Infrastructure.Data.Repositories.AuditEventRepository.GetChainAsync"/>
/// sorts by <c>OccurredAt</c> on the client side. Two events created within the same
/// clock tick (possible in tests and under high concurrency in production) produce
/// non-deterministic ordering, which makes chain verification order-sensitive.
/// This test mitigates the risk by introducing a 1 ms delay between event creations.
/// The definitive fix would add a monotone sequence column to <c>AuditEvents</c>
/// (deferred — would require a new migration and DB lock).
/// </para>
/// </summary>
public sealed class AuditChainIntegrityTest : AuditRepositoryTestBase
{
    // -------------------------------------------------------------------------
    // Chain consistency — N-event round-trip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a chain of N audit events persisted via <see cref="AuditEventRepository"/>
    /// satisfies the invariant: <c>chain[i].PreviousHash == chain[i-1].StateHash</c> for all i &gt; 0,
    /// and <c>chain[0].PreviousHash == "GENESIS"</c>.
    /// </summary>
    [Fact]
    public async Task AuditChain_NEvents_ChainIsConsistent()
    {
        const int chainLength = 5;
        var tenantId = Guid.NewGuid();
        var repository = new AuditEventRepository(AuditContext);

        var previousHash = "GENESIS";

        for (var i = 0; i < chainLength; i++)
        {
            var stateHash = ComputeTestHash($"event-{i}-{tenantId}");

            var auditEvent = AuditEvent.Create(
                tenantId:     tenantId,
                eventType:    $"TestEvent{i}",
                aggregateId:  tenantId,
                payload:      $"{{\"seq\":{i}}}",
                stateHash:    stateHash,
                previousHash: previousHash);

            await repository.AddAsync(auditEvent, TestContext.Current.CancellationToken);
            previousHash = stateHash;

            // 1 ms gap guarantees distinct OccurredAt timestamps so GetChainAsync ordering is stable.
            // See class-level doc for the known ordering limitation.
            await Task.Delay(1, TestContext.Current.CancellationToken);
        }

        await AuditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AuditContext.ChangeTracker.Clear();

        // Act
        var chain = await repository.GetChainAsync(
            tenantId,
            from: null,
            to: null,
            ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(chainLength, chain.Count);
        Assert.Equal("GENESIS", chain[0].PreviousHash);

        for (var i = 1; i < chain.Count; i++)
            Assert.Equal(chain[i - 1].StateHash, chain[i].PreviousHash);
    }

    // -------------------------------------------------------------------------
    // GetLastHashAsync — returns latest StateHash
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="AuditEventRepository.GetLastHashAsync"/> returns the
    /// <see cref="AuditEvent.StateHash"/> of the most recently created event for the given tenant.
    /// </summary>
    [Fact]
    public async Task AuditChain_GetLastHash_ReturnsLatestStateHash()
    {
        var tenantId = Guid.NewGuid();
        var repository = new AuditEventRepository(AuditContext);

        var stateHash1 = ComputeTestHash("last-hash-event-1");
        var stateHash2 = ComputeTestHash("last-hash-event-2");

        var event1 = AuditEvent.Create(tenantId, "Event1", tenantId, "{}", stateHash1, "GENESIS");
        await repository.AddAsync(event1, TestContext.Current.CancellationToken);
        await Task.Delay(1, TestContext.Current.CancellationToken);

        var event2 = AuditEvent.Create(tenantId, "Event2", tenantId, "{}", stateHash2, stateHash1);
        await repository.AddAsync(event2, TestContext.Current.CancellationToken);
        await AuditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AuditContext.ChangeTracker.Clear();

        // Act
        var lastHash = await repository.GetLastHashAsync(tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(stateHash2, lastHash);
    }

    // -------------------------------------------------------------------------
    // Tamper detection — broken chain is detectable
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a chain with a tampered <see cref="AuditEvent.PreviousHash"/>
    /// — i.e. the link does not match the preceding event's <see cref="AuditEvent.StateHash"/> —
    /// is detectable from the stored data. This confirms that a verifier reading the chain
    /// would catch tampering or an out-of-order insert.
    /// </summary>
    [Fact]
    public async Task AuditChain_TamperedPreviousHash_BreakIsDetectable()
    {
        var tenantId = Guid.NewGuid();
        var repository = new AuditEventRepository(AuditContext);

        var stateHash1 = ComputeTestHash("tamper-event-1");
        // event2's PreviousHash deliberately does NOT equal event1.StateHash
        var tamperedPreviousHash = "0000000000000000000000000000000000000000000000000000000000000000";

        var event1 = AuditEvent.Create(tenantId, "Event1", tenantId, "{}", stateHash1, "GENESIS");
        await repository.AddAsync(event1, TestContext.Current.CancellationToken);
        await Task.Delay(1, TestContext.Current.CancellationToken);

        var event2 = AuditEvent.Create(
            tenantId, "Event2", tenantId, "{}",
            stateHash: ComputeTestHash("tamper-event-2"),
            previousHash: tamperedPreviousHash);  // tampered link

        await repository.AddAsync(event2, TestContext.Current.CancellationToken);
        await AuditContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        AuditContext.ChangeTracker.Clear();

        // Act
        var chain = await repository.GetChainAsync(tenantId, null, null, TestContext.Current.CancellationToken);

        // Assert: the break is detectable — chain[1].PreviousHash != chain[0].StateHash
        Assert.Equal(2, chain.Count);
        Assert.NotEqual(chain[0].StateHash, chain[1].PreviousHash);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ComputeTestHash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
