// SpaceOS.Kernel.Tests/Domain/ISnapshotableInterfaceTests.cs

using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Domain;

/// <summary>
/// Tests verifying the <see cref="ISnapshotable"/> interface contract and its implementation on domain aggregates.
/// </summary>
public sealed class ISnapshotableInterfaceTests
{
    private static readonly TenantId   T = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId F = FacilityId.From(Guid.NewGuid());

    [Fact]
    public void FlowEpic_IsAssignableTo_ISnapshotable()
    {
        var epic = FlowEpic.Create("Test", F, T);
        Assert.IsAssignableFrom<ISnapshotable>(epic);
    }

    [Fact]
    public void ToSnapshotJson_DifferentEpics_ProduceDifferentJson()
    {
        ISnapshotable e1 = FlowEpic.Create("Alpha", F, T);
        ISnapshotable e2 = FlowEpic.Create("Beta",  F, T);

        Assert.NotEqual(e1.ToSnapshotJson(), e2.ToSnapshotJson());
    }

    [Fact]
    public void ToSnapshotJson_SameEpicCalledTwice_ProducesIdenticalJson()
    {
        ISnapshotable epic = FlowEpic.Create("Same", F, T);

        var json1 = epic.ToSnapshotJson();
        var json2 = epic.ToSnapshotJson();

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void ToSnapshotJson_AfterMutation_JsonChanges()
    {
        var epic = FlowEpic.Create("Before", F, T);
        ISnapshotable snapshot = epic;
        var before = snapshot.ToSnapshotJson();

        epic.UpdateTitle("After");
        var after = snapshot.ToSnapshotJson();

        Assert.NotEqual(before, after);
    }
}
