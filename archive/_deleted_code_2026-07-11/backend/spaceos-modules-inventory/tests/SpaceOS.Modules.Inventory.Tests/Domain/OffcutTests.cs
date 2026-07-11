using FluentAssertions;
using SpaceOS.Modules.Inventory.Domain.Aggregates;
using SpaceOS.Modules.Inventory.Domain.Enums;
using SpaceOS.Modules.Inventory.Domain.Events;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Domain;

public class OffcutTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CatalogId = Guid.NewGuid();

    [Fact]
    public void Register_ShouldSetStatusAvailable()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.Status.Should().Be(OffcutStatus.Available);
    }

    [Fact]
    public void Register_ShouldRaiseOffcutRegisteredEvent()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.DomainEvents.Should().ContainSingle(e => e is OffcutRegisteredEvent);
    }

    [Fact]
    public void MarkUsed_ShouldTransitionToUsed()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.MarkUsed();
        offcut.Status.Should().Be(OffcutStatus.Used);
    }

    [Fact]
    public void MarkWaste_ShouldTransitionToWaste()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.MarkWaste();
        offcut.Status.Should().Be(OffcutStatus.Waste);
    }

    [Fact]
    public void MarkUsed_WhenAlreadyUsed_ShouldThrow()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.MarkUsed();
        var act = () => offcut.MarkUsed();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkWaste_WhenUsed_ShouldThrow()
    {
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, null);
        offcut.MarkUsed();
        var act = () => offcut.MarkWaste();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Register_WithOriginSheetId_ShouldPreserveIt()
    {
        var sheetId = Guid.NewGuid();
        var offcut = Offcut.Register(TenantId, CatalogId, 500, 300, sheetId);
        offcut.OriginCuttingSheetId.Should().Be(sheetId);
    }
}
