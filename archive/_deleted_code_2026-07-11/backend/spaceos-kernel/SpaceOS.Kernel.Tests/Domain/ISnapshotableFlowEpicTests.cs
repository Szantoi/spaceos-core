// SpaceOS.Kernel.Tests/Domain/ISnapshotableFlowEpicTests.cs

using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Unit tests verifying that <see cref="FlowEpic"/> correctly implements <see cref="ISnapshotable"/>
/// and that its snapshot DTO captures private-setter fields (BE-P3B-01).
/// </summary>
public sealed class ISnapshotableFlowEpicTests
{
    private static readonly TenantId   SomeTenantId   = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId SomeFacilityId = FacilityId.From(Guid.NewGuid());

    // ── ToSnapshotDto ─────────────────────────────────────────────────────────

    [Fact]
    public void ToSnapshotDto_NewEpic_CapturesTitle()
    {
        var epic = FlowEpic.Create("My Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.Equal("My Epic", dto.Title);
    }

    [Fact]
    public void ToSnapshotDto_NewEpic_CapturesTenantId()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.Equal(SomeTenantId.Value, dto.TenantId);
    }

    [Fact]
    public void ToSnapshotDto_NewEpic_CapturesFacilityId()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.Equal(SomeFacilityId.Value, dto.TargetFacilityId);
    }

    [Fact]
    public void ToSnapshotDto_NewEpic_PhaseIsDiscovery()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.Equal("Discovery", dto.Phase);
    }

    [Fact]
    public void ToSnapshotDto_NewEpic_IsArchivedIsFalse()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.False(dto.IsArchived);
    }

    [Fact]
    public void ToSnapshotDto_NewEpic_ProofHashIsNull()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var dto  = epic.ToSnapshotDto();
        Assert.Null(dto.ProofHash);
    }

    [Fact]
    public void ToSnapshotDto_AfterClose_CapturesProofHash()
    {
        var epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        epic.StartExecution();
        epic.Close("http://proof.example/file.pdf", "cafecafe" + new string('a', 56));

        var dto = epic.ToSnapshotDto();
        Assert.NotNull(dto.ProofHash);
        Assert.Equal("cafecafe" + new string('a', 56), dto.ProofHash);
    }

    // ── ISnapshotable.ToSnapshotJson ─────────────────────────────────────────

    [Fact]
    public void ToSnapshotJson_ReturnsNonEmptyString()
    {
        ISnapshotable epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var json = epic.ToSnapshotJson();
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public void ToSnapshotJson_NotEmptyObject()
    {
        // Empty JSON object {} would mean private setters were not captured
        ISnapshotable epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var json = epic.ToSnapshotJson();
        Assert.NotEqual("{}", json.Trim());
    }

    [Fact]
    public void ToSnapshotJson_ContainsTitleValue()
    {
        ISnapshotable epic = FlowEpic.Create("SnapshotTitle", SomeFacilityId, SomeTenantId);
        var json = epic.ToSnapshotJson();
        Assert.Contains("SnapshotTitle", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ToSnapshotJson_ContainsPhase()
    {
        ISnapshotable epic = FlowEpic.Create("Epic", SomeFacilityId, SomeTenantId);
        var json = epic.ToSnapshotJson();
        Assert.Contains("Discovery", json, StringComparison.Ordinal);
    }
}
