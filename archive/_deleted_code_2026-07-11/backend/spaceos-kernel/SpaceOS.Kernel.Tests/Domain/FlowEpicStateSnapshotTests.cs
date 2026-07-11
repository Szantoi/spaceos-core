// SpaceOS.Kernel.Tests/Domain/FlowEpicStateSnapshotTests.cs

using SpaceOS.Kernel.Domain.Snapshots;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Tests for the <see cref="FlowEpicStateSnapshot"/> DTO record.
/// Verifies that the record is a proper value type and that field mapping is correct.
/// </summary>
public sealed class FlowEpicStateSnapshotTests
{
    [Fact]
    public void FlowEpicStateSnapshot_DefaultFormatVersion_IsTwo()
    {
        var snap = new FlowEpicStateSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), "Epic", "Discovery",
            false, null, null, null, Guid.NewGuid(), null, null);

        Assert.Equal(2, snap.SnapshotFormatVersion);
    }

    [Fact]
    public void FlowEpicStateSnapshot_ExplicitFormatVersion_IsPreserved()
    {
        var snap = new FlowEpicStateSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), "Epic", "Discovery",
            false, null, null, null, Guid.NewGuid(), null, null, SnapshotFormatVersion: 3);

        Assert.Equal(3, snap.SnapshotFormatVersion);
    }

    [Fact]
    public void FlowEpicStateSnapshot_RecordEquality_SameValues_AreEqual()
    {
        var id       = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var facilId  = Guid.NewGuid();

        var snap1 = new FlowEpicStateSnapshot(id, tenantId, "Epic", "Discovery", false, null, null, null, facilId, null, null);
        var snap2 = new FlowEpicStateSnapshot(id, tenantId, "Epic", "Discovery", false, null, null, null, facilId, null, null);

        Assert.Equal(snap1, snap2);
    }

    [Fact]
    public void FlowEpicStateSnapshot_RecordEquality_DifferentTitle_AreNotEqual()
    {
        var id       = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var facilId  = Guid.NewGuid();

        var snap1 = new FlowEpicStateSnapshot(id, tenantId, "Alpha", "Discovery", false, null, null, null, facilId, null, null);
        var snap2 = new FlowEpicStateSnapshot(id, tenantId, "Beta",  "Discovery", false, null, null, null, facilId, null, null);

        Assert.NotEqual(snap1, snap2);
    }

    [Fact]
    public void FlowEpicStateSnapshot_WithScope_PreservesScope()
    {
        var snap = new FlowEpicStateSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), "Cabinet Assembly", "Discovery",
            false, null, null, null, Guid.NewGuid(), "MicroAssembly", "Senior");

        Assert.Equal("MicroAssembly", snap.Scope);
        Assert.Equal("Senior", snap.RequiredSkillLevel);
    }
}
