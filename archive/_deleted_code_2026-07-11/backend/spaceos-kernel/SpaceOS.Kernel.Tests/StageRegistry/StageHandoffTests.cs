// SpaceOS.Kernel.Tests/StageRegistry/StageHandoffTests.cs
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Events;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry;

/// <summary>Unit tests for <see cref="StageHandoff"/> immutable creation and hash behaviour.</summary>
public sealed class StageHandoffTests
{
    private static readonly Guid TenantId     = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FlowEpicId   = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid IdempotencyKey = new("30000000-0000-0000-0000-000000000003");

    private static StageHandoff Build(
        Guid? tenantId        = null,
        Guid? flowEpicId      = null,
        string source         = "stage_a",
        string target         = "stage_b",
        int version           = 1,
        Guid? idempotencyKey  = null,
        string payload        = "{\"key\":\"value\"}")
        => StageHandoff.Create(
            tenantId       ?? TenantId,
            flowEpicId     ?? FlowEpicId,
            source,
            target,
            version,
            idempotencyKey ?? IdempotencyKey,
            payload,
            sourceActorId: null,
            targetActorId: null,
            handshakeId:   null);

    // ─── HashAlgorithm field ──────────────────────────────────────────────────

    [Fact]
    public void Create_HashAlgorithmField_IsSHA256()
    {
        var handoff = Build();

        Assert.Equal("SHA-256", handoff.HashAlgorithm);
    }

    // ─── Hash determinism ─────────────────────────────────────────────────────

    [Fact]
    public void Create_HashIsDeterministic_SameInputsProduceSameHash()
    {
        var h1 = Build();
        var h2 = Build();

        Assert.Equal(h1.PayloadHash, h2.PayloadHash);
    }

    [Fact]
    public void Create_HashIsDifferent_DifferentVersionsProduceDifferentHash()
    {
        var h1 = Build(version: 1);
        var h2 = Build(version: 2);

        Assert.NotEqual(h1.PayloadHash, h2.PayloadHash);
    }

    [Fact]
    public void Create_HashIsDifferent_DifferentPayloadsProduceDifferentHash()
    {
        var h1 = Build(payload: "{\"a\":1}");
        var h2 = Build(payload: "{\"a\":2}");

        Assert.NotEqual(h1.PayloadHash, h2.PayloadHash);
    }

    [Fact]
    public void Create_HashIsDifferent_DifferentTenantIdsProduceDifferentHash()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var h1 = Build(tenantId: tenant1);
        var h2 = Build(tenantId: tenant2);

        Assert.NotEqual(h1.PayloadHash, h2.PayloadHash);
    }

    // ─── PayloadHash format ───────────────────────────────────────────────────

    [Fact]
    public void Create_PayloadHash_IsLowercaseHex64Chars()
    {
        var handoff = Build();

        Assert.Equal(64, handoff.PayloadHash.Length);
        Assert.Matches("^[0-9a-f]{64}$", handoff.PayloadHash);
    }

    // ─── IdempotencyKey ───────────────────────────────────────────────────────

    [Fact]
    public void Create_IdempotencyKey_StoredOnEntity()
    {
        var key = Guid.NewGuid();
        var handoff = Build(idempotencyKey: key);

        Assert.Equal(key, handoff.IdempotencyKey);
    }

    // ─── Domain event ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_RaisesStageHandoffCreatedEvent()
    {
        var handoff = Build();

        var events = handoff.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<StageHandoffCreatedEvent>(events[0]);
        Assert.Equal(handoff.Id, evt.Id);
        Assert.Equal(FlowEpicId, evt.FlowEpicId);
        Assert.Equal("stage_a", evt.Source);
        Assert.Equal("stage_b", evt.Target);
    }

    // ─── Properties stored correctly ─────────────────────────────────────────

    [Fact]
    public void Create_VersionStoredCorrectly()
    {
        var handoff = Build(version: 7);

        Assert.Equal(7, handoff.Version);
    }

    [Fact]
    public void Create_FlowEpicIdStoredCorrectly()
    {
        var epicId = Guid.NewGuid();
        var handoff = Build(flowEpicId: epicId);

        Assert.Equal(epicId, handoff.FlowEpicId);
    }
}
