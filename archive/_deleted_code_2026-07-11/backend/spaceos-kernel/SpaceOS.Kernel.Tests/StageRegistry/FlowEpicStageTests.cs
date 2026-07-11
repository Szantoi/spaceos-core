// SpaceOS.Kernel.Tests/StageRegistry/FlowEpicStageTests.cs
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry;

/// <summary>Unit tests for <see cref="FlowEpic"/> stage-chain assignment and advancement methods.</summary>
public sealed class FlowEpicStageTests
{
    private static readonly TenantId   TestTenant   = TenantId.From(new Guid("50000000-0000-0000-0000-000000000005"));
    private static readonly FacilityId TestFacility = FacilityId.From(new Guid("60000000-0000-0000-0000-000000000006"));

    private static FlowEpic NewEpic() =>
        FlowEpic.Create("Test Epic", TestFacility, TestTenant);

    // ─── AssignChain ──────────────────────────────────────────────────────────

    [Fact]
    public void AssignChain_Valid_SetsStageChainTemplateId()
    {
        var epic          = NewEpic();
        var chainId       = Guid.NewGuid();
        const string code = "stage_a";

        epic.AssignChain(chainId, code);

        Assert.Equal(chainId, epic.StageChainTemplateId);
        Assert.Equal(code, epic.CurrentStageCode);
    }

    [Fact]
    public void AssignChain_CalledTwice_ThrowsDomainException()
    {
        var epic    = NewEpic();
        var chainId = Guid.NewGuid();
        epic.AssignChain(chainId, "stage_a");

        Assert.Throws<DomainException>(() =>
            epic.AssignChain(Guid.NewGuid(), "stage_b"));
    }

    [Fact]
    public void AssignChain_Valid_RaisesFlowEpicStageAdvancedEventWithNullFrom()
    {
        var epic    = NewEpic();
        epic.PopDomainEvents(); // clear creation event
        var chainId = Guid.NewGuid();

        epic.AssignChain(chainId, "stage_a");

        var events = epic.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<FlowEpicStageAdvancedEvent>(events[0]);
        Assert.Null(evt.From);
        Assert.Equal("stage_a", evt.To);
    }

    // ─── AdvanceToStage ───────────────────────────────────────────────────────

    [Fact]
    public void AdvanceToStage_Valid_UpdatesCurrentStageCode()
    {
        var epic = NewEpic();
        epic.AssignChain(Guid.NewGuid(), "stage_a");
        epic.PopDomainEvents();

        epic.AdvanceToStage("stage_b");

        Assert.Equal("stage_b", epic.CurrentStageCode);
    }

    [Fact]
    public void AdvanceToStage_Valid_RaisesFlowEpicStageAdvancedEvent()
    {
        var epic = NewEpic();
        epic.AssignChain(Guid.NewGuid(), "stage_a");
        epic.PopDomainEvents();

        epic.AdvanceToStage("stage_b");

        var events = epic.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<FlowEpicStageAdvancedEvent>(events[0]);
        Assert.Equal("stage_a", evt.From);
        Assert.Equal("stage_b", evt.To);
        Assert.Equal(epic.Id.Value, evt.FlowEpicId);
    }

    // ─── SkipOptionalStage ────────────────────────────────────────────────────

    [Fact]
    public void SkipOptionalStage_Valid_RaisesFlowEpicStageSkippedEvent()
    {
        var epic = NewEpic();
        epic.AssignChain(Guid.NewGuid(), "stage_a");
        epic.PopDomainEvents();

        epic.SkipOptionalStage("stage_b");

        var events = epic.PopDomainEvents();
        Assert.Single(events);
        var evt = Assert.IsType<FlowEpicStageSkippedEvent>(events[0]);
        Assert.Equal("stage_b", evt.Skipped);
        Assert.Equal(epic.Id.Value, evt.FlowEpicId);
    }
}
