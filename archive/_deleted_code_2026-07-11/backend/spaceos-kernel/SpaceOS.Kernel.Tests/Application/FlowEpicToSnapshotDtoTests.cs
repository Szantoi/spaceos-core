// SpaceOS.Kernel.Tests/Application/FlowEpicToSnapshotDtoTests.cs

using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>
/// Tests verifying that <see cref="FlowEpic.ToSnapshotDto()"/> captures all domain state
/// correctly for use in the snapshot pipeline (BE-P3B-01).
/// </summary>
public sealed class FlowEpicToSnapshotDtoTests
{
    private static readonly TenantId   T = TenantId.From(Guid.NewGuid());
    private static readonly FacilityId F = FacilityId.From(Guid.NewGuid());

    [Fact]
    public void ToSnapshotDto_AfterDelegation_CapturesGuestTenantId()
    {
        // Arrange
        var epic        = FlowEpic.Create("Delegated", F, T);
        var guestTenant = TenantId.From(Guid.NewGuid());
        epic.DelegateTo(guestTenant);

        // Act
        var dto = epic.ToSnapshotDto();

        // Assert
        Assert.NotNull(dto.HandshakeGuestTenantId);
        Assert.Equal(guestTenant.Value, dto.HandshakeGuestTenantId!.Value);
    }

    [Fact]
    public void ToSnapshotDto_NoDelegation_HandshakeGuestTenantIdIsNull()
    {
        var epic = FlowEpic.Create("NoDelegation", F, T);
        var dto  = epic.ToSnapshotDto();
        Assert.Null(dto.HandshakeGuestTenantId);
    }

    [Fact]
    public void ToSnapshotDto_ArchivedEpic_IsArchivedIsTrue()
    {
        var epic = FlowEpic.Create("ToArchive", F, T);
        epic.Archive();
        var dto = epic.ToSnapshotDto();
        Assert.True(dto.IsArchived);
    }

    [Fact]
    public void ToSnapshotDto_AfterTitleUpdate_NewTitleCaptured()
    {
        var epic = FlowEpic.Create("OldTitle", F, T);
        epic.UpdateTitle("NewTitle");
        var dto = epic.ToSnapshotDto();
        Assert.Equal("NewTitle", dto.Title);
    }

    [Fact]
    public void ToSnapshotDto_AfterStartExecution_PhaseIsDelivery()
    {
        var epic = FlowEpic.Create("InDelivery", F, T);
        epic.StartExecution();
        var dto = epic.ToSnapshotDto();
        Assert.Equal("Delivery", dto.Phase);
    }

    [Fact]
    public void ToSnapshotDto_EpicIdMatchesEpicId()
    {
        var epic = FlowEpic.Create("Epic", F, T);
        var dto  = epic.ToSnapshotDto();
        Assert.Equal(epic.Id.Value, dto.EpicId);
    }
}
